using System.Text;

namespace BandBridge.Data
{
    /// <summary>
    /// Circular buffer. In case of the end of free space, old data will be ovewritten.
    /// </summary>
    public class CircularBuffer
    {
        #region Fields
        /// <summary>
        /// Table represents the buffer.
        /// </summary>
        private int[] buffer;

        /// <summary>
        /// The index of currently active buffer element.
        /// </summary>
        private int iterator;

        /// <summary>
        /// The capacity of the buffer.
        /// </summary>
        private int capacity;

        /// <summary>
        /// Is the buffer full?
        /// </summary>
        private bool isFull;
        #endregion

        #region Properties
        /// <summary>
        /// The capacity of the buffer.
        /// </summary>
        public int Capacity
        {
            get { return capacity; }
        }

        /// <summary>
        /// Is the buffer full?
        /// </summary>
        public bool IsFull
        {
            get { return isFull; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="CircularBuffer"/>.
        /// </summary>
        /// <param name="size">Size of the buffer</param>
        public CircularBuffer(int size)
        {
            capacity = size;
            buffer = new int[capacity];
            iterator = 0;
            isFull = false;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds new value to the buffer.
        /// </summary>
        /// <param name="obj">Value to add</param>
        public void Add(int obj)
        {
            buffer[iterator] = obj;
            iterator = ++iterator % buffer.Length;
            if (iterator == 0) isFull = true;
        }

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public void Clear()
        {
            buffer = new int[capacity];
            iterator = 0;
            isFull = false;
        }

        /// <summary>
        /// Resizes the buffer.
        /// </summary>
        /// <param name="newSize">New size of the buffer</param>
        public void Resize(int newSize)
        {
            capacity = newSize;
            Clear();
        }

        /// <summary>
        /// Returns the average of values currently stored in buffer.
        /// </summary>
        /// <returns>The average of values stored in buffer</returns>
        public int GetAverage()
        {
            int sum = 0, itemsCount = 0;
            foreach (int obj in buffer)
            {
                if (obj > 0)
                {
                    sum += obj;
                    itemsCount++;
                }
            }
            if (itemsCount > 0)
                return sum / itemsCount;
            else
                return 0;
        }

        /// <summary>
        /// Writes all buffer elements in form of: [a1 | a2 | ... an | ]
        /// </summary>
        /// <returns>CircularBuffer in string format</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[ ");
            foreach (int item in buffer)
            {
                sb.Append(item + " | ");
            }
            sb.Append("]");
            return sb.ToString();
        }
        #endregion
    }
}
