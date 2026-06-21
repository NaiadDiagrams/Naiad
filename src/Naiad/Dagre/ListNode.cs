namespace Naiad.Dagre;

/// <summary>Base for entries threaded into a <see cref="DoublyLinkedList"/> (port of list.ts's ListNode).</summary>
class ListNode
{
    public ListNode? Next;
    public ListNode? Prev;
}