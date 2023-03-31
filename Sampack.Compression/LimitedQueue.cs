namespace Sampack.Compression;

internal class LimitedQueue<T>
{
    private readonly Queue<T> queue;
    public int Limit { get; }

    public LimitedQueue(int limit)
    {
        queue = new Queue<T>(limit);
        Limit = limit;
    }

    public void AddRange(IEnumerable<T> values)
    {
        foreach(var value in values)
        {
            queue.Enqueue(value);

            while(queue.Count > Limit)
            {
                queue.Dequeue();
            }
        }
    }

    public T[] ToArray() => queue.ToArray();
}
