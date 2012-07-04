using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents a 2D grid of type T. 
/// This is really just a convenient accessor into a 1D array.
/// </summary>
public class Grid<T> : IEnumerable<T> where T : new()
{
    private int width;
    private int height;
    private T[] items;

    public Grid(int width, int height, T[] items)
    {
        this.width = width;
        this.height = height;
        this.items = items;
    }

    public Grid(int width, int height)
        : this(width, height, new T[width * height])
    {

    }

    public T this[int x, int y]
    {
        get
        {
            return items[y * width + x];
        }
        set
        {
            items[y * width + x] = value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        foreach (T t in items)
        {
            yield return t;
        }
    }
}