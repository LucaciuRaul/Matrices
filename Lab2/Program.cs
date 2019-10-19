using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace Lab2
{
    class Matrix
    {
        public int Row { get; set; }
        public int Column { get; set; }
        double[,] arr;
        public static Mutex mutex = new Mutex();
        protected static readonly Object locker = new Object();

        Matrix() { }
        public Matrix(int row, int column)
        {
            Row = row;
            Column = column;
            arr = new double[row, column];
        }
        public double[] GetColumn(int i)
        {
            double[] res = new double[Row];
            for (int j = 0; j < Row; j++)
                res[j] = arr[j, i];
            return res;
        }
        public double[] GetRow(int i)
        {
            double[] res = new double[Column];
            for (int j = 0; j < Column; j++)
                res[j] = arr[i, j];
            return res;
        }
        public double this[int i, int j]
        {
            get { return arr[i, j]; }
            set { arr[i, j] = value; }
        }
        public Matrix RandomValues()
        {
            Random rnd = new Random();
            for (int i = 0; i < Row; i++)
                for (int j = 0; j < Column; j++)
                    arr[i, j] = rnd.Next(10);
            return this;
        }

        public void Print()
        {
            Console.WriteLine("Matrix:");
            for (int i = 0; i < Row; i++)
            {
                for (int j = 0; j < Column; j++)
                    Console.Write(arr[i, j] + " ");
                Console.WriteLine();
            }
        }

        //Multiplication using Threads --> Done if the matrix rank is > 10, then we divide even the rows and column and multiply them in separate Threads
        public static Matrix operator *(Matrix a, Matrix b)
        {
            Matrix result = new Matrix(a.Row, b.Column);
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < a.Row; i++)
                for (int j = 0; j < b.Column; j++)
                {
                    int tempi = i;
                    int tempj = j;
                    if (a.Column <= 5)
                    {
                        Thread thread = new Thread(() => VectorMult(tempi, tempj, a.GetRow(tempi), b.GetColumn(tempj), result));
                        thread.Start();
                        threads.Add(thread);
                    }
                    else
                    {
                        for (int l = 0; l <= a.Column; l += 5)
                        {
                            var auxA = a.GetRow(i).Skip(5);
                            var auxB = b.GetColumn(j).Skip(5);
                            Thread thread = new Thread(() => VectorMult(
                                tempi, tempj,
                                auxA.Take(5).ToArray(),
                                auxB.Take(5).ToArray(),
                                result));
                            thread.Start();
                            threads.Add(thread);
                        }
                    }
                }
            foreach (Thread t in threads)
                t.Join();
            return result;
        }

        //Multiplication using ThreadPool
        public static Matrix operator |(Matrix a, Matrix b)
        {
            Matrix result = new Matrix(a.Row, b.Column);
            var list = new List<int>();
            ManualResetEvent[] handles = new ManualResetEvent[a.Row * b.Column];
            for (int i = 0; i < a.Row; i++)
            {
                for (int j = 0; j < b.Column; j++)
                {
                    list.Add(i * a.Column + j);
                    handles[i * a.Column + j] = new ManualResetEvent(false);
                }
            }

            for (int i = 0; i < a.Row; i++)
                for (int j = 0; j < b.Column; j++)
                {
                    int tempi = i;
                    int tempj = j;
                    ThreadPool.QueueUserWorkItem(VectorMultThreadPool, new object[] { tempi, tempj, a, b, result, list, handles });
                }

            WaitHandle.WaitAll(handles);

            return result;
        }

        //Multiplication using Taks
        public static Matrix operator /(Matrix a, Matrix b)
        {
            Matrix result = new Matrix(a.Row, b.Column);
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < a.Row; i++)
                for (int j = 0; j < b.Column; j++)
                {
                    int tempi = i;
                    int tempj = j;
                    Task task = new Task(() => VectorMultTasks(tempi, tempj, a, b, result));
                    task.Start();
                    tasks.Add(task);
                }
            foreach (Task t in tasks)
                t.Wait();
            return result;
        }

        /// -------------------MULTIPLICATION-------------------

        //Addition using Threads
        public static Matrix operator +(Matrix a, Matrix b)
        {
            Matrix result = new Matrix(a.Row, b.Column);
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < a.Row; i++)
            {
                int tempi = i;
                Thread thread = new Thread(() => VectorAdd(tempi, a, b, result));
                thread.Start();
                threads.Add(thread);
            }
            foreach (Thread t in threads)
                t.Join();
            return result;
        }

        //Addition using ThreadPool
        public static Matrix operator &(Matrix a, Matrix b)
        {
            Matrix result = new Matrix(a.Row, b.Column);
            var list = new List<int>();
            ManualResetEvent[] handles = new ManualResetEvent[a.Row];
            for (int i = 0; i < a.Row; i++)
            {
                list.Add(i);
                handles[i] = new ManualResetEvent(false);
            }

            for (int i = 0; i < a.Row; i++)
            {
                int tempi = i;
                ThreadPool.QueueUserWorkItem(VectorAddThreadPool, new object[] { tempi, a, b, result, list, handles });
            }
            WaitHandle.WaitAll(handles);

            return result;
        }

        //Addition using Tasks
        public static Matrix operator -(Matrix a, Matrix b)
        {
            Matrix result = new Matrix(a.Row, b.Column);
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < a.Row; i++)
            {
                int tempi = i;
                Task task = new Task(() => VectorAddTasks(tempi, a, b, result));
                task.Start();
                tasks.Add(task);
            }
            foreach (Task t in tasks)
                t.Wait();
            return result;
        }

        public static void VectorAdd(int tmpi, Matrix a, Matrix b, Matrix result)
        {
            //mutex.WaitOne();

            double[] x = a.GetRow(tmpi);
            double[] y = b.GetRow(tmpi);

            for (int k = 0; k < x.Length; k++)
                result[tmpi, k] += x[k] + y[k];

            //mutex.ReleaseMutex();
        }

        public static void VectorAddThreadPool(object state)
        {
            object[] array = state as object[];
            int tmpi = (int)array[0];
            Matrix a = (Matrix)array[1];
            Matrix b = (Matrix)array[2];
            Matrix result = (Matrix)array[3];
            List<int> list = (List<int>)array[4];
            ManualResetEvent[] handles = (ManualResetEvent[])array[5];

            handles[(int)tmpi].Set();

            double[] x = a.GetRow(tmpi);
            double[] y = b.GetRow(tmpi);

            for (int k = 0; k < x.Length; k++)
                result[tmpi, k] += x[k] + y[k];
        }

        public static void VectorAddTasks(int tmpi, Matrix a, Matrix b, Matrix result)
        {
            double[] x = a.GetRow(tmpi);
            double[] y = b.GetRow(tmpi);

            for (int k = 0; k < x.Length; k++)
                result[tmpi, k] += x[k] + y[k];
        }

        public static void VectorMult(int tmpi, int tmpj, double[] a, double[] b, Matrix result)
        {
            mutex.WaitOne();

            int i = tmpi;
            int j = tmpj;
            double[] x = a;
            double[] y = b;

            for (int k = 0; k < x.Length; k++)
                result[i, j] += x[k] * y[k];

            mutex.ReleaseMutex();
        }

        public static void VectorMultThreadPool(object state)
        {
            object[] array = state as object[];
            int tmpi = (int)array[0];
            int tmpj = (int)array[1];
            Matrix a = (Matrix)array[2];
            Matrix b = (Matrix)array[3];
            Matrix result = (Matrix)array[4];
            List<int> list = (List<int>)array[5];
            ManualResetEvent[] handles = (ManualResetEvent[])array[6];

            double[] x = a.GetRow(tmpi);
            double[] y = b.GetColumn(tmpj);

            for (int k = 0; k < x.Length; k++)
                result[tmpi, tmpj] += x[k] * y[k];

            handles[(int)tmpi * a.Column + (int)tmpj].Set();
        }

        public static void VectorMultTasks(int tmpi, int tmpj, Matrix a, Matrix b, Matrix result)
        {
            int i = tmpi;
            int j = tmpj;
            double[] x = a.GetRow(i);
            double[] y = b.GetColumn(j);

            for (int k = 0; k < x.Length; k++)
                result[i, j] += x[k] * y[k];
        }
    }

    class Program
    {
        static Stopwatch sw = new Stopwatch();
        static TimeSpan time1, time2, time3;

        static void Main(string[] args)
        {
            Console.Write("n=");
            int n = int.Parse(Console.ReadLine());
            Matrix A = new Matrix(n, n).RandomValues();
            Matrix B = new Matrix(n, n).RandomValues();

            A.Print();
            Console.WriteLine(new String('-', 20));
            B.Print();
            Console.WriteLine(new String('-', 20));

            ///Threads 
            Console.WriteLine("\nThreads -->...\n");

            sw.Start();
            Matrix C = A * B;
            sw.Stop();
            time1 = sw.Elapsed;

            C.Print();
            Matrix D = A + B;
            D.Print();

            ///Tasks -> Here ' & ' and ' | ' operations represent the ' + ' and ' * ' operations but done with ThreadPool
            Console.WriteLine("\nThreadPool -->...\n");
            sw.Restart();
            Matrix G = A | B;
            sw.Stop();
            time2 = sw.Elapsed;

            G.Print();
            Matrix H = A & B;
            H.Print();

            ///Tasks -> Here ' - ' and ' / ' operations represent as well the ' + ' and ' * ' operations but done with Tasks
            Console.WriteLine("\nTasks -->...\n");
            sw.Restart();
            Matrix E = A / B;
            sw.Stop();
            time3 = sw.Elapsed;

            E.Print();
            Matrix F = A - B;
            F.Print();

            Console.WriteLine("\n\nProgram ended. Timing:\n   Threads:" + time1 + " s\nThreadPool:" + time2 + " s\n     Tasks:" + time3 + " s.");
            Console.ReadLine();
        }
    }
}