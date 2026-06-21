namespace Naiad.Dagre.Tests;

// Port of dagre's test/data/list-test.ts.
// The C# port models list entries as `ListNode` subclasses (the TS uses plain objects with a
// `[key: string]: unknown` index signature). We use small typed payload subclasses so the FIFO
// behaviour can be observed via reference equality, exactly like the TS `toBe(obj)` checks.
public class ListTests
{
    DoublyLinkedList list = null!;

    [Before(Test)]
    public void Setup() => list = new();

    sealed class Entry : ListNode
    {
        public int Value;
    }

    [Test]
    public async Task ReturnsUndefinedWithAnEmptyList() =>
        await Assert.That(list.Dequeue()).IsNull();

    [Test]
    public async Task UnlinksAndReturnsTheFirstEntry()
    {
        var obj = new Entry();
        list.Enqueue(obj);
        await Assert.That(list.Dequeue()).IsSameReferenceAs(obj);
    }

    [Test]
    public async Task UnlinksAndReturnsMultipleEntriesInFifoOrder()
    {
        var obj1 = new Entry();
        var obj2 = new Entry();
        list.Enqueue(obj1);
        list.Enqueue(obj2);

        await Assert.That(list.Dequeue()).IsSameReferenceAs(obj1);
        await Assert.That(list.Dequeue()).IsSameReferenceAs(obj2);
    }

    [Test]
    public async Task UnlinksAndRelinksAnEntryIfItIsReEnqueued()
    {
        var obj1 = new Entry();
        var obj2 = new Entry();
        list.Enqueue(obj1);
        list.Enqueue(obj2);
        list.Enqueue(obj1);

        await Assert.That(list.Dequeue()).IsSameReferenceAs(obj2);
        await Assert.That(list.Dequeue()).IsSameReferenceAs(obj1);
    }

    [Test]
    public async Task UnlinksAndRelinksAnEntryIfItIsEnqueuedOnAnotherList()
    {
        var obj = new Entry();
        var list2 = new DoublyLinkedList();
        list.Enqueue(obj);
        list2.Enqueue(obj);

        await Assert.That(list.Dequeue()).IsNull();
        await Assert.That(list2.Dequeue()).IsSameReferenceAs(obj);
    }

    // The TS "can return a string representation" test relies on `toString()` /
    // JSON.stringify of an arbitrary entry. The C# port (DoublyLinkedList) has no ToString
    // override and entries are strongly-typed payloads, so there is no faithful equivalent;
    // skipped.
}
