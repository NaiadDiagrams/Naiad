namespace Naiad.Dagre;

/// <summary>
/// Simple doubly linked list (Cormen et al.). Used by the
/// greedy feedback-arc-set heuristic. Enqueue adds at the front; dequeue removes from the back.
/// </summary>
sealed class DoublyLinkedList
{
    readonly ListNode sentinel = new();

    public DoublyLinkedList()
    {
        sentinel.Next = sentinel;
        sentinel.Prev = sentinel;
    }

    public ListNode? Dequeue()
    {
        var entry = sentinel.Prev;
        if (entry != sentinel)
        {
            Unlink(entry!);
            return entry;
        }

        return null;
    }

    public void Enqueue(ListNode entry)
    {
        if (entry is {Prev: not null, Next: not null})
        {
            Unlink(entry);
        }

        entry.Next = sentinel.Next;
        sentinel.Next!.Prev = entry;
        sentinel.Next = entry;
        entry.Prev = sentinel;
    }

    static void Unlink(ListNode entry)
    {
        entry.Prev!.Next = entry.Next;
        entry.Next!.Prev = entry.Prev;
        entry.Next = null;
        entry.Prev = null;
    }
}
