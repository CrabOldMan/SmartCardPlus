﻿using System.Diagnostics.Contracts;

namespace System.Security.Cryptography
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public class HMACSHA224Base : KeyedHashAlgorithm
    {
        //
        // protected members
        //

        // an HMAC uses a hash function where data is hashed by iterating a basic compression 
        // function on blocks of data. BlockSizeValue is the byte size of such a block

        private int blockSizeValue = 64;

        protected int BlockSizeValue
        {
            get
            {
                return blockSizeValue;
            }
            set
            {
                blockSizeValue = value;
            }
        }

        internal string m_hashName = "SHA224";

        internal SHA224Managed m_hash1 = new SHA224Managed();
        internal SHA224Managed m_hash2 = new SHA224Managed();

        //
        // private members
        //

        // m_inner = PaddedKey ^ {0x36,...,0x36}
        // m_outer = PaddedKey ^ {0x5C,...,0x5C}
        private byte[] m_inner;
        private byte[] m_outer;

        private bool m_hashing = false;

        private void UpdateIOPadBuffers()
        {
            if (m_inner == null)
                m_inner = new byte[BlockSizeValue];
            if (m_outer == null)
                m_outer = new byte[BlockSizeValue];

            int i;
            for (i = 0; i < BlockSizeValue; i++)
            {
                m_inner[i] = 0x36;
                m_outer[i] = 0x5C;
            }
            for (i = 0; i < KeyValue.Length; i++)
            {
                m_inner[i] ^= KeyValue[i];
                m_outer[i] ^= KeyValue[i];
            }
        }

        internal void InitializeKey(byte[] key)
        {
            // When we change the key value, we'll need to update the initial values of the inner and outter
            // computation buffers.  In the case of correct HMAC vs Whidbey HMAC, these buffers could get
            // generated to a different size than when we started.
            m_inner = null;
            m_outer = null;

            if (key.Length > BlockSizeValue)
            {
                KeyValue = m_hash1.ComputeHash(key);
                // No need to call Initialize, ComputeHash will do it for us
            }
            else
            {
                KeyValue = (byte[])key.Clone();
            }
            UpdateIOPadBuffers();
        }

        //
        // public properties
        //

        public override byte[] Key
        {
            get { return (byte[])KeyValue.Clone(); }
            set
            {
                if (m_hashing)
                    throw new CryptographicException("Cryptography_HashKeySet");
                InitializeKey(value);
            }
        }

        

        //
        // public methods
        //

        

        public override void Initialize()
        {
            m_hash1.Initialize();
            m_hash2.Initialize();
            m_hashing = false;
        }

        protected override void HashCore(byte[] rgb, int ib, int cb)
        {
            if (m_hashing == false)
            {
                m_hash1.TransformBlock(m_inner, 0, m_inner.Length, m_inner, 0);
                m_hashing = true;
            }
            m_hash1.TransformBlock(rgb, ib, cb, rgb, ib);
        }

        internal static class EmptyArray<T>
        {
            public static readonly T[] Value = new T[0];
        }

        protected override byte[] HashFinal()
        {
            if (m_hashing == false)
            {
                m_hash1.TransformBlock(m_inner, 0, m_inner.Length, m_inner, 0);
                m_hashing = true;
            }
            // finalize the original hash
            m_hash1.TransformFinalBlock(EmptyArray<Byte>.Value, 0, 0);
            byte[] hashValue1 = m_hash1.HashValueSHA224;
            // write the outer array
            m_hash2.TransformBlock(m_outer, 0, m_outer.Length, m_outer, 0);
            // write the inner hash and finalize the hash
            m_hash2.TransformBlock(hashValue1, 0, hashValue1.Length, hashValue1, 0);
            m_hashing = false;
            m_hash2.TransformFinalBlock(EmptyArray<Byte>.Value, 0, 0);
            return m_hash2.HashValueSHA224;
        }

        //
        // IDisposable methods
        //

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_hash1 != null)
                    ((IDisposable)m_hash1).Dispose();
                if (m_hash2 != null)
                    ((IDisposable)m_hash2).Dispose();
                if (m_inner != null)
                    Array.Clear(m_inner, 0, m_inner.Length);
                if (m_outer != null)
                    Array.Clear(m_outer, 0, m_outer.Length);
            }
            // call the base class's Dispose
            base.Dispose(disposing);
        }
    }
}