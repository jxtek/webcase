﻿using System;

namespace WebReady
{
    /// <summary>
    /// Represents a provider or input source of dataset, a data object, or some of its data fields.
    /// </summary>
    public interface ISource
    {
        bool Get(string name, ref bool v);

        bool Get(string name, ref char v);

        bool Get(string name, ref short v);

        bool Get(string name, ref int v);

        bool Get(string name, ref long v);

        bool Get(string name, ref double v);

        bool Get(string name, ref decimal v);

        bool Get(string name, ref DateTime v);

        bool Get(string name, ref string v);

        bool Get(string name, ref ArraySegment<byte> v);

        bool Get(string name, ref Guid v);

        bool Get(string name, ref byte[] v);

        bool Get(string name, ref short[] v);

        bool Get(string name, ref int[] v);

        bool Get(string name, ref long[] v);

        bool Get(string name, ref string[] v);

        bool Get(string name, ref JObj v);

        bool Get(string name, ref JArr v);

        bool Get<D>(string name, ref D v, byte proj = 0x0f) where D : IData, new();

        bool Get<D>(string name, ref D[] v, byte proj = 0x0f) where D : IData, new();

        void Let(out bool v);

        void Let(out char v);

        void Let(out short v);

        void Let(out int v);

        void Let(out long v);

        void Let(out double v);

        void Let(out decimal v);

        void Let(out DateTime v);

        void Let(out string v);

        void Let(out ArraySegment<byte> v);

        void Let(out Guid v);

        void Let(out short[] v);

        void Let(out int[] v);

        void Let(out long[] v);

        void Let(out string[] v);

        void Let(out JObj v);

        void Let(out JArr v);

        void Let<D>(out D v, byte proj = 0x0f) where D : IData, new();

        void Let<D>(out D[] v, byte proj = 0x0f) where D : IData, new();

        D ToObject<D>(byte proj = 0x0f) where D : IData, new();

        D[] ToArray<D>(byte proj = 0x0f) where D : IData, new();

        /// <summary>
        /// If this input source contains multiple data records.
        /// </summary>
        bool DataSet { get; }

        /// <summary>
        /// Move to next data records.
        /// </summary>
        /// <returns>True if sucessfully moved to next data record.</returns>
        bool Next();

        /// <summary>
        /// Outputs current data object.
        /// </summary>
        void Write<C>(C cnt) where C : IContent, ISink;

        /// <summary>
        /// Converts this source into corresponding content object.
        /// </summary>
        /// <returns></returns>
        IContent Dump();
    }
}