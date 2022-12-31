using System.Diagnostics;

namespace Gameboy.Tests;

public class StopwatchCollectionTest
{
	private class MockStopwatch : StopwatchCollection.IStopwatch
	{
		public TimeSpan Elapsed { get; set; }

		public void Start()
		{
			throw new NotImplementedException();
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}
	}

	private class MockStopwatchProvider : StopwatchCollection.IStopwatchProvider
	{
		public StopwatchCollection.IStopwatch Create()
		{
			return new MockStopwatch();
		}
	}

	[Fact]
	public void Empty()
	{
		var collection = new StopwatchCollection(new MockStopwatchProvider());
		Assert.Equal(
			"",
			collection.ToString()
		);
	}

	[Fact]
	public void SomeEntriesWithParents()
	{
		var collection = new StopwatchCollection(new MockStopwatchProvider());

		var foo = (MockStopwatch)collection.Get("foo");
		foo.Elapsed = TimeSpan.FromSeconds(5);
		var bar = (MockStopwatch)collection.Get("bar", "foo");
		bar.Elapsed = TimeSpan.FromSeconds(2);
		var baz = (MockStopwatch)collection.Get("baz", "foo");
		baz.Elapsed = TimeSpan.FromSeconds(1);

		var asdf = (MockStopwatch)collection.Get("asdf");
		asdf.Elapsed = TimeSpan.FromSeconds(3);

		// unresolved parent, won't appear in output
		var swizzle = (MockStopwatch)collection.Get("swizzle", "whizz");
		swizzle.Elapsed = TimeSpan.FromSeconds(2);

		Assert.Equal(
			"""
			asdf = 00:00:03
			foo = 00:00:05
			    bar = 00:00:02 (40.00%)
			    baz = 00:00:01 (20.00%)
			""",
			collection.ToString()
		);
	}

	[Fact]
	public void ContainsOthers()
	{
		var collection = new StopwatchCollection(new MockStopwatchProvider());

		var a = (MockStopwatch)collection.Get("a", null, new string[] { "b", "c" });
		a.Elapsed = TimeSpan.FromSeconds(10);
		var b = (MockStopwatch)collection.Get("b");
		b.Elapsed = TimeSpan.FromSeconds(2);
		var c = (MockStopwatch)collection.Get("c");
		c.Elapsed = TimeSpan.FromSeconds(3);

		var d = (MockStopwatch)collection.Get("d");
		d.Elapsed = TimeSpan.FromSeconds(10);
		var e = (MockStopwatch)collection.Get("e", "d");
		e.Elapsed = TimeSpan.FromSeconds(2);
		var f = (MockStopwatch)collection.Get("f", "d", new string[] { "e", "g" });
		f.Elapsed = TimeSpan.FromSeconds(8);
		var g = (MockStopwatch)collection.Get("g", "d");
		g.Elapsed = TimeSpan.FromSeconds(3);

		Assert.Equal(
			"""
			a = 00:00:05
			b = 00:00:02
			c = 00:00:03
			d = 00:00:10
			    e = 00:00:02 (20.00%)
			    f = 00:00:03 (30.00%)
			    g = 00:00:03 (30.00%)
			""",
			collection.ToString()
		);
	}
}