﻿using System;
using System.Threading;
using System.Collections.Generic;

namespace Lab2
{
    class Matrix
    {
        public int Row { get; set; }
        public int Column { get; set; }
        double[,] arr;
        public static Mutex mutex = new Mutex();

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

        public static Matrix operator *(Matrix a, Matrix b)
        {
            Matrix result = new Matrix(a.Row, b.Column);
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < a.Row; i++)
                for (int j = 0; j < b.Column; j++)
                {
                    int tempi = i;
                    int tempj = j;
                    Thread thread = new Thread(() => VectorMult(tempi, tempj, a, b, result));
                    thread.Start();
                    threads.Add(thread);
                }
            foreach (Thread t in threads)
                t.Join();
            return result;
        }

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

        public static void VectorAdd(int tmpi, Matrix a, Matrix b, Matrix result)
        {
            mutex.WaitOne();

            double[] x = a.GetRow(tmpi);
            double[] y = b.GetRow(tmpi);

            for (int k = 0; k < x.Length; k++)
                result[tmpi, k] += x[k] + y[k];

            mutex.ReleaseMutex();
        }

        public static void VectorMult(int tmpi, int tmpj, Matrix a, Matrix b, Matrix result)
        {
            mutex.WaitOne();

            int i = tmpi;
            int j = tmpj;
            double[] x = a.GetRow(i);
            double[] y = b.GetColumn(j);

            for (int k = 0; k < x.Length; k++)
                result[i, j] += x[k] * y[k];

            mutex.ReleaseMutex();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("n=");
            int n = int.Parse(Console.ReadLine());
            Console.Write("m=");
            int m = int.Parse(Console.ReadLine());
            Console.Write("k=");
            int k = int.Parse(Console.ReadLine());
            Matrix A = new Matrix(n, m).RandomValues();
            Matrix B = new Matrix(m, k).RandomValues();
            A.Print();
            Console.WriteLine(new String('-', 20));
            B.Print();
            Console.WriteLine(new String('-', 20));
            //Matrix C = A * B;
            //C.Print();
            Matrix D = A + B;
            D.Print();
            Console.ReadLine();
        }
    }
}