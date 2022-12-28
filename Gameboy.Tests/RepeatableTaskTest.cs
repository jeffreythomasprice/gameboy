using Microsoft.Extensions.Logging;

namespace Gameboy.Tests;

public class RepeatableTaskTest
{
	private class CounterThread : RepeatableTask
	{
		private readonly ILogger logger;

		public bool IsDisposed { get; private set; } = false;
		public int Counter { get; private set; } = 0;

		public TimeSpan Delay = TimeSpan.Zero;

		public CounterThread(ILoggerFactory loggerFactory) : base(loggerFactory)
		{
			logger = loggerFactory.CreateLogger<CounterThread>();
		}

		protected override void DisposeImpl()
		{
			IsDisposed = true;
		}

		protected override async Task ThreadRunImpl(CancellationToken cancellationToken)
		{
			await Task.Delay(Delay, cancellationToken);
			Counter++;
		}
	}

	[Fact]
	public async Task RepeatedStarts()
	{
		var task = new CounterThread(LoggerUtils.CreateLoggerFactory());
		task.Delay = TimeSpan.FromMilliseconds(200);

		Assert.Equal(0, task.Counter);
		Assert.False(task.IsDisposed);
		Assert.True(task.IsCompleted);

		task.Start();
		Assert.Equal(0, task.Counter);
		Assert.False(task.IsDisposed);
		Assert.False(task.IsCompleted);

		await task.WaitAsync(CancellationToken.None);
		Assert.Equal(1, task.Counter);
		Assert.False(task.IsDisposed);
		Assert.True(task.IsCompleted);

		task.Start();
		Assert.Equal(1, task.Counter);
		Assert.False(task.IsDisposed);
		Assert.False(task.IsCompleted);

		await task.WaitAsync(CancellationToken.None);
		Assert.Equal(2, task.Counter);
		Assert.False(task.IsDisposed);
		Assert.True(task.IsCompleted);

		task.Dispose();
		Assert.Equal(2, task.Counter);
		Assert.True(task.IsDisposed);
		Assert.True(task.IsCompleted);
	}

	[Fact]
	public async Task Cancelling()
	{
		var task = new CounterThread(LoggerUtils.CreateLoggerFactory());
		task.Delay = TimeSpan.FromHours(999);
		task.Start();
		try
		{
			await task.WaitAsync(TimeSpan.FromMilliseconds(200));
		}
		catch (TimeoutException)
		{
			// expected
		}
		Assert.Equal(0, task.Counter);
		Assert.False(task.IsCompleted);
		task.Stop();
		await task.WaitAsync(CancellationToken.None);
		Assert.Equal(0, task.Counter);
		Assert.True(task.IsCompleted);
	}
}