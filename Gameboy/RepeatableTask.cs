using Microsoft.Extensions.Logging;

namespace Gameboy;

public abstract class RepeatableTask : IDisposable
{
	private bool isDisposed = false;

	private readonly ILogger logger;

	private CancellationTokenSource? cancellationTokenSource;
	private TaskCompletionSource? taskCompletionSource;

	public RepeatableTask(ILoggerFactory loggerFactory)
	{
		logger = loggerFactory.CreateLogger<RepeatableTask>();
	}

	~RepeatableTask()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(true);
	}

	public bool IsCompleted => taskCompletionSource?.Task.IsCompleted ?? true;

	public void Start()
	{
		lock (this)
		{
			if (cancellationTokenSource != null)
			{
				logger.LogTrace("task is already running");
				return;
			}
			cancellationTokenSource = new CancellationTokenSource();
			taskCompletionSource = new TaskCompletionSource();
			var cancellationToken = cancellationTokenSource.Token;
			Task.Run(async () =>
			{
				try
				{
					await ThreadRunImpl(cancellationToken);
				}
				catch (OperationCanceledException)
				{
					// expected, no need to log
				}
				catch (Exception e)
				{
					logger.LogError(e, "error performing task");
				}
				finally
				{
					lock (this)
					{
						cancellationTokenSource = null;
						logger.LogTrace("task stopped");
						taskCompletionSource?.TrySetResult();
					}
				}
			});
		}
	}

	public void Stop()
	{
		lock (this)
		{
			cancellationTokenSource?.Cancel();
		}
	}

	public void Wait(CancellationToken cancellationToken)
	{
		taskCompletionSource?.Task.Wait(cancellationToken);
	}

	public void Wait(TimeSpan timeout)
	{
		taskCompletionSource?.Task.Wait(timeout);
	}

	public Task WaitAsync(CancellationToken cancellationToken)
	{
		return taskCompletionSource?.Task.WaitAsync(cancellationToken) ?? Task.CompletedTask;
	}

	public Task WaitAsync(TimeSpan timeout)
	{
		return taskCompletionSource?.Task.WaitAsync(timeout) ?? Task.CompletedTask;
	}

	private void Dispose(bool disposing)
	{
		try
		{
			if (isDisposed)
			{
				return;
			}
			isDisposed = true;
			Stop();
			taskCompletionSource?.Task.Wait();
			DisposeImpl();
		}
		catch (Exception e)
		{
			logger.LogError(e, "error disposing");
		}
	}

	protected abstract void DisposeImpl();
	protected abstract Task ThreadRunImpl(CancellationToken cancellationToken);
}
