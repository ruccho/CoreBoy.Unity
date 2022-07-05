using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreBoy.gpu
{
    public class Fifo<T>
    {
        private T[] buffer;

        private int head = 0;
        private int tail = 0;
        public int Count => (head - tail + buffer.Length) % buffer.Length;

        public Fifo() : this(4)
        {
        }

        public Fifo(int capacity)
        {
            buffer = new T[capacity];
        }

        private int TranslateIndex(int queueIndex)
        {
            if (queueIndex < 0 || Count <= queueIndex) throw new ArgumentOutOfRangeException();

            return (tail + queueIndex + buffer.Length) % buffer.Length;
        }

        public T Get(int index)
        {
            return buffer[TranslateIndex(index)];
        }

        public void Set(int index, T value)
        {
            buffer[TranslateIndex(index)] = value;
        }

        public void Clear()
        {
            head = 0;
            tail = 0;
        }

        public void Enqueue(T value)
        {
            int remain = buffer.Length - Count;

            if (remain <= 1)
            {
                Expand(buffer.Length + 1);
            }

            buffer[head] = value;
            head = (head + 1) % buffer.Length;
        }

        public void Enqueue(Span<T> sequence)
        {
            int remain = buffer.Length - Count;

            if (remain <= sequence.Length)
            {
                Expand(buffer.Length + sequence.Length);
            }
            
            //to end
            int toEnd = buffer.Length - head;
            int fromStart = sequence.Length - toEnd;

            int toEndActual = Math.Min(toEnd, sequence.Length);
            int fromStartActual = sequence.Length - toEnd;
            
            sequence.Slice(0, toEndActual).CopyTo(buffer.AsSpan(head, toEndActual));
            if(fromStartActual > 0) sequence.Slice(toEnd, fromStartActual).CopyTo(buffer.AsSpan(0, fromStartActual));
            
            head = (head + sequence.Length) % buffer.Length;
        }

        public T Dequeue()
        {
            int remain = buffer.Length - Count;

            if (remain <= 1) throw new InvalidOperationException();

            var result = buffer[tail];
            
            tail = (tail + 1) % buffer.Length;

            return result;
        }

        private void Expand(int required)
        {
            int r = required;
            int d = 0;
            for (; r > 0; r >>= 1) d++;

            int size = 1 << d;

            if (size <= buffer.Length) return;

            var old = buffer;
            buffer = new T[size];

            if (tail == head) return;
            
            if (tail < head)
            {
                Array.Copy(old, 0, buffer, 0, old.Length);
            }
            else
            {
                Array.Copy(old, 0, buffer, 0, head);

                int tailLength = old.Length - tail;
                int newTail = tail = size - tailLength;
                Array.Copy(old, tail, buffer, newTail, tailLength);

                tail = newTail;
            }
        }
    }
}