using System.Diagnostics;
using System.Text;

namespace Gameboy;

public class StopwatchCollection
{
	public interface IStopwatch
	{
		TimeSpan Elapsed { get; }
		void Start();
		void Stop();
		void Reset();
	}

	public interface IStopwatchProvider
	{
		IStopwatch Create();
	}

	private class DefaultStopwatch : IStopwatch
	{
		private Stopwatch instance = new();

		public TimeSpan Elapsed => instance.Elapsed;

		public void Start()
		{
			instance.Start();
		}

		public void Stop()
		{
			instance.Stop();
		}

		public void Reset()
		{
			instance.Reset();
		}
	}

	private class DefaultStopwatchProvider : IStopwatchProvider
	{
		public IStopwatch Create()
		{
			return new DefaultStopwatch();
		}
	}

	private class EntryReference
	{
		private IReadOnlyDictionary<string, Entry> entries;

		public string Name { get; private init; }
		public Entry? Entry { get; private set; }

		public EntryReference(IReadOnlyDictionary<string, Entry> entries, string name)
		{
			this.entries = entries;
			this.Name = name;
			Resolve();
		}

		public bool IsResolved => Entry != null;

		public void Resolve()
		{
			if (Entry == null && entries.TryGetValue(Name, out var entry))
			{
				this.Entry = entry;
			}
		}
	}

	private class Entry
	{
		private IReadOnlyDictionary<string, Entry> entries;

		public string Name { get; private init; }
		public EntryReference? Parent { get; private init; }
		public IReadOnlyCollection<EntryReference> ContainsOthers { get; private init; }
		public IStopwatch Stopwatch { get; private init; }

		public Entry(IStopwatchProvider provider, IReadOnlyDictionary<string, Entry> entries, string name, EntryReference? parent, IReadOnlyCollection<EntryReference>? containsOthers)
		{
			this.entries = entries;
			this.Name = name;
			this.Parent = parent;
			this.ContainsOthers = containsOthers ?? new List<EntryReference>();
			this.Stopwatch = provider.Create();
		}

		public override string ToString()
		{
			var elapsed = Stopwatch.Elapsed;
			foreach (var other in ContainsOthers)
			{
				var otherElapsed = other.Entry?.Stopwatch.Elapsed;
				if (otherElapsed != null)
				{
					elapsed -= otherElapsed.Value;
				}
			}
			var result = $"{Name} = {elapsed}";
			if (Parent != null)
			{
				var parent = this.Parent.Entry;
				if (parent != null)
				{
					var percentage = elapsed.TotalNanoseconds / parent.Stopwatch.Elapsed.TotalNanoseconds * 100.0;
					result += $" ({percentage:N2}%)";
				}
				else
				{
					result += $""" (parent "{Parent.Name}" is not resolved)""";
				}
			}
			return result;
		}

		public bool IsResolved
		{
			get
			{
				if (Parent?.IsResolved == false)
				{
					return false;
				}
				foreach (var other in ContainsOthers)
				{
					if (!other.IsResolved)
					{
						return false;
					}
				}
				return true;
			}
		}

		public void Resolve()
		{
			Parent?.Resolve();
			foreach (var other in ContainsOthers)
			{
				other.Resolve();
			}
		}

		public IEnumerable<Entry> Children =>
			entries.Values.Where(e => e.Parent?.Name == Name);
	}

	private readonly IStopwatchProvider provider;
	private readonly Dictionary<string, Entry> entries = new();

	public StopwatchCollection(IStopwatchProvider? provider = null)
	{
		this.provider = provider != null ? provider : new DefaultStopwatchProvider();
	}

	public override string ToString()
	{
		string single(Entry entry, int indent)
		{
			var result = "";
			for (var i = 0; i < indent; i++)
			{
				result += "    ";
			}
			result += entry.ToString();
			var children = entry.Children.ToList();
			if (children.Count > 0)
			{
				result += "\n";
				result += list(children, indent + 1);
			}
			return result;
		}

		string list(IEnumerable<Entry> entries, int indent)
		{
			var sorted = entries.ToList();
			sorted.Sort((a, b) => a.Name.CompareTo(b.Name));
			return string.Join("\n", sorted.Select(e => single(e, indent)));
		}

		return list(entries.Values.Where(e => e.Parent == null), 0);
	}

	public IStopwatch Get(string name, string? parent = null, string[]? containsOthers = null)
	{
		lock (this)
		{
			if (!entries.TryGetValue(name, out var result))
			{
				result = new(
					provider,
					entries,
					name,
					parent != null ? new(entries, parent) : null,
					containsOthers?.Select(other => new EntryReference(entries, other)).ToList()
				);
				entries[name] = result;
				foreach (var e in entries.Values)
				{
					e.Resolve();
				}
			}
			return result.Stopwatch;
		}
	}
}