﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ICSharpCode.SharpZipLib.Checksums;

using VGMToolbox.util;
using VGMToolbox.util.ObjectPooling;

namespace VGMToolbox.format 
{
    class Nsfe : IFormat
    {
        private static readonly byte[] ASCII_SIGNATURE = new byte[] { 0x4E, 0x53, 0x46, 0x45 }; // NSFE
        private const string FORMAT_ABBREVIATION = "NSFE";

        struct ChunkStruct
        {
            public byte[] chunkSize;
            public byte[] chunkIdentifier;
            public byte[] chunkData;
        }

        private static readonly byte[] INFO_SIGNATURE = new byte[] { 0x49, 0x4E, 0x46, 0x4F }; // INFO
        private static readonly byte[] DATA_SIGNATURE = new byte[] { 0x44, 0x41, 0x54, 0x41 }; // DATA
        private static readonly byte[] NEND_SIGNATURE = new byte[] { 0x4E, 0x45, 0x4E, 0x44 }; // NEND

        private static readonly byte[] PLST_SIGNATURE = new byte[] { 0x70, 0x6C, 0x73, 0x74 }; // plst
        private static readonly byte[] TIME_SIGNATURE = new byte[] { 0x74, 0x69, 0x6D, 0x65 }; // time
        private static readonly byte[] FADE_SIGNATURE = new byte[] { 0x66, 0x61, 0x64, 0x65 }; // fade
        private static readonly byte[] TRACK_LABELS_SIGNATURE = new byte[] { 0x74, 0x6C, 0x62, 0x6C }; // tlbl
        private static readonly byte[] AUTH_SIGNATURE = new byte[] { 0x61, 0x75, 0x74, 0x68 }; // auth
        private static readonly byte[] BANK_SIGNATURE = new byte[] { 0x42, 0x41, 0x4E, 0x4B }; // BANK

        private static readonly byte[] NULL_TERMINATOR = new byte[] { 0x00 };

        private const int SIG_OFFSET = 0x00;
        private const int SIG_LENGTH = 0x04;

        // CHUNK Offsets
        private const int INITIAL_CHUNK_OFFSET = 0x04;

        private const int CHUNK_SIZE_OFFSET = 0x00;
        private const int CHUNK_SIZE_SIZE = 0x04;

        private const int CHUNK_IDENTIFIER_OFFSET = 0x04;
        private const int CHUNK_IDENTIFIER_SIZE = 0x04;

        private const int CHUNK_DATA_OFFSET = 0x08;

        // SIGNATURE
        private byte[] asciiSignature;

        // INFO Chunk
        private const int LOAD_ADDRESS_OFFSET = 0x00;
        private const int LOAD_ADDRESS_LENGTH = 0x02;

        private const int INIT_ADDRESS_OFFSET = 0x02;
        private const int INIT_ADDRESS_LENGTH = 0x02;

        private const int PLAY_ADDRESS_OFFSET = 0x04;
        private const int PLAY_ADDRESS_LENGTH = 0x02;

        private const int PAL_NTSC_BITS_OFFSET = 0x06;
        private const int PAL_NTSC_BITS_LENGTH = 0x01;

        private const int EXTRA_SOUND_BITS_OFFSET = 0x07;
        private const int EXTRA_SOUND_BITS_LENGTH = 0x01;

        private const int TOTAL_SONGS_OFFSET = 0x08;
        private const int TOTAL_SONGS_LENGTH = 0x01;

        private const int STARTING_SONG_OFFSET = 0x09;
        private const int STARTING_SONG_LENGTH = 0x01;

        private byte[] loadAddress;
        private byte[] initAddress;
        private byte[] playAddress;
        private byte[] palNtscBits;
        private byte[] extraChipsBits;
        private byte[] totalSongs;
        private byte[] startingSong;

        // plst Chunk
        private string playlist = String.Empty;
        public string Playlist { get { return playlist; } }

        // DATA Chunk
        private byte[] data;

        // time Chunk
        private Int32[] times;
        public Int32[] Times { get { return times; } }

        // fade Chunk
        private Int32[] fades;
        public Int32[] Fades { get { return fades; } }

        // tlbl Chunk
        private ArrayList trackLabels = new ArrayList();
        public string[] TrackLabels { get { return (string[])trackLabels.ToArray(typeof(string)); } }


        // auth Chunk
        private string songName;
        private string songArtist;
        private string songCopyright;
        private string nsfRipper;

        public string SongName { get { return songName; } }
        public string SongArtist { get { return songArtist; } }
        public string SongCopyright { get { return songCopyright; } }
        public string NsfRipper { get { return nsfRipper; } }

        // BANK Chunk
        private byte[] bankSwitchInit;

        private ArrayList chunks = new ArrayList();
        Dictionary<string, string> tagHash = new Dictionary<string, string>();        
                                                       
        #region METHODS
        public byte[] getAsciiSignature(Stream pStream)
        {
            return ParseFile.parseSimpleOffset(pStream, SIG_OFFSET, SIG_LENGTH);
        }

        public void getChunks(Stream pStream)
        {
            int offset = INITIAL_CHUNK_OFFSET;

            while (offset < pStream.Length)
            {
                ChunkStruct newChunk = new ChunkStruct();
                UInt16 dataLength;

                // Chunk Size
                newChunk.chunkSize = new byte[CHUNK_SIZE_SIZE];
                pStream.Seek(offset + CHUNK_SIZE_OFFSET, SeekOrigin.Begin);
                pStream.Read(newChunk.chunkSize, 0, CHUNK_SIZE_SIZE);

                // Chunk Id
                newChunk.chunkIdentifier = new byte[CHUNK_IDENTIFIER_SIZE];
                pStream.Seek(offset + CHUNK_IDENTIFIER_OFFSET, SeekOrigin.Begin);
                pStream.Read(newChunk.chunkIdentifier, 0, CHUNK_IDENTIFIER_SIZE);

                // Chunk Data
                dataLength = BitConverter.ToUInt16(newChunk.chunkSize, 0);
                newChunk.chunkData = new byte[dataLength];
                pStream.Seek(offset + CHUNK_DATA_OFFSET, SeekOrigin.Begin);
                pStream.Read(newChunk.chunkData, 0, dataLength);

                // Add Chunk to Array
                chunks.Add(newChunk);
                
                // Increment Offset
                offset += CHUNK_DATA_OFFSET + (int) dataLength;
            }
        }

        public void Initialize(Stream pStream)
        {
            this.asciiSignature = this.getAsciiSignature(pStream);
            this.getChunks(pStream);

            this.parseInfoChunk();
            this.parsePlaylistChunk();
            this.parseDataChunk();
            this.parseTimesChunk();
            this.parseFadesChunk();
            this.parseTrackLabelsChunk();
            this.parseAuthChunk();
            this.parseBankChunk();

            this.initializeTagHash();
        }

        private void initializeTagHash()
        {
            int i = 0;
            System.Text.Encoding enc = System.Text.Encoding.ASCII;

            tagHash.Add("Name", this.songName);
            tagHash.Add("Artist", this.songArtist);
            tagHash.Add("Copyright", this.songCopyright);
            tagHash.Add("Ripper", this.nsfRipper);

            tagHash.Add("Total Songs", this.totalSongs[0].ToString());
            tagHash.Add("Starting Song", this.startingSong[0].ToString());
            tagHash.Add("Playlist", this.playlist);

            // Track Labels            
            foreach (string s in this.trackLabels)
            {
                int tempTime = this.times[i] + this.fades[i];
                int minutes = tempTime / 60000;
                int seconds = ((tempTime - (minutes * 60000)) % 60000) / 1000;
                tagHash.Add("Track " + i++, (string)s + " [" + minutes + ":" + seconds.ToString("d2") + "]");

            }
        }

        private void parseInfoChunk()
        {
            foreach (ChunkStruct c in chunks)
            { 
                if (ParseFile.CompareSegment(c.chunkIdentifier, 0, INFO_SIGNATURE))
                {
                    this.loadAddress = ParseFile.parseSimpleOffset(c.chunkData, LOAD_ADDRESS_OFFSET, LOAD_ADDRESS_LENGTH);
                    this.initAddress = ParseFile.parseSimpleOffset(c.chunkData, INIT_ADDRESS_OFFSET, INIT_ADDRESS_LENGTH);
                    this.playAddress = ParseFile.parseSimpleOffset(c.chunkData, PLAY_ADDRESS_OFFSET, PLAY_ADDRESS_LENGTH);
                    this.palNtscBits = ParseFile.parseSimpleOffset(c.chunkData, PAL_NTSC_BITS_OFFSET, PAL_NTSC_BITS_LENGTH);
                    this.extraChipsBits = ParseFile.parseSimpleOffset(c.chunkData, EXTRA_SOUND_BITS_OFFSET, EXTRA_SOUND_BITS_LENGTH);
                    this.totalSongs = ParseFile.parseSimpleOffset(c.chunkData, TOTAL_SONGS_OFFSET, TOTAL_SONGS_LENGTH);
                    this.startingSong = ParseFile.parseSimpleOffset(c.chunkData, STARTING_SONG_OFFSET, STARTING_SONG_LENGTH);
                }
            }
        }
        private void parsePlaylistChunk()
        {
            foreach (ChunkStruct c in chunks)
            {
                if (ParseFile.CompareSegment(c.chunkIdentifier, 0, PLST_SIGNATURE))
                {
                    for (int i = 0; i < c.chunkData.Length; i++)
                    {
                        this.playlist += c.chunkData[i].ToString() + ", ";
                    }
                    this.playlist = this.playlist.Substring(0, this.playlist.Length - 2);
                }
            }
        }        
        private void parseDataChunk()
        {
            foreach (ChunkStruct c in chunks)
            {
                if (ParseFile.CompareSegment(c.chunkIdentifier, 0, DATA_SIGNATURE))
                {
                    this.data = c.chunkData;
                }
            }
        }
        private void parseTimesChunk()
        {
            foreach (ChunkStruct c in chunks)
            {
                if (ParseFile.CompareSegment(c.chunkIdentifier, 0, TIME_SIGNATURE))
                {
                    this.times = new Int32[c.chunkData.Length/4];
                    int j = 0;

                    for (int i = 0; i < c.chunkData.Length; i+=4)
                    {
                        byte[] tempTime = ParseFile.parseSimpleOffset(c.chunkData, i, 4);
                        this.times[j] = BitConverter.ToInt32(tempTime, 0);
                        j++;
                    }
                    
                }
            }
        }
        private void parseFadesChunk()
        {
            foreach (ChunkStruct c in chunks)
            {
                if (ParseFile.CompareSegment(c.chunkIdentifier, 0, FADE_SIGNATURE))
                {
                    this.fades = new Int32[c.chunkData.Length/4];
                    int j = 0;
                    
                    for (int i = 0; i < c.chunkData.Length; i+=4)
                    {
                        byte[] tempTime = ParseFile.parseSimpleOffset(c.chunkData, i, 4);
                        this.fades[j] = BitConverter.ToInt32(tempTime, 0);
                        j++;
                    }

                }
            }
        }
        private void parseTrackLabelsChunk()
        {                        
            foreach (ChunkStruct c in chunks)
            {
                if (ParseFile.CompareSegment(c.chunkIdentifier, 0, TRACK_LABELS_SIGNATURE))
                {
                    System.Text.Encoding enc = System.Text.Encoding.ASCII;
                    int offset = 0;
    
                    while (offset < c.chunkData.Length)
                    {
                        int labelLength = ParseFile.getSegmentLength(c.chunkData, offset, NULL_TERMINATOR);
                        byte[] trackLabel = ParseFile.parseSimpleOffset(c.chunkData, offset, labelLength);
                        this.trackLabels.Add(enc.GetString(trackLabel));

                        offset += labelLength + 1;
                    }
                }
            }
        }        
        private void parseAuthChunk()
        {
            foreach (ChunkStruct c in chunks)
            {
                if (ParseFile.CompareSegment(c.chunkIdentifier, 0, AUTH_SIGNATURE))
                {
                    System.Text.Encoding enc = System.Text.Encoding.ASCII;
                    byte[] authBlock;
                    int offset = 0;
                    int labelLength;


                    if (offset < c.chunkData.Length)
                    { 
                        labelLength = ParseFile.getSegmentLength(c.chunkData, offset, NULL_TERMINATOR);
                        authBlock = ParseFile.parseSimpleOffset(c.chunkData, offset, labelLength);
                        songName = enc.GetString(authBlock);
                        offset += labelLength + 1;
                    }

                    if (offset < c.chunkData.Length)
                    { 
                        labelLength = ParseFile.getSegmentLength(c.chunkData, offset, NULL_TERMINATOR);
                        authBlock = ParseFile.parseSimpleOffset(c.chunkData, offset, labelLength);
                        songArtist = enc.GetString(authBlock);
                        offset += labelLength + 1;
                    }                    

                    if (offset < c.chunkData.Length)
                    { 
                        labelLength = ParseFile.getSegmentLength(c.chunkData, offset, NULL_TERMINATOR);
                        authBlock = ParseFile.parseSimpleOffset(c.chunkData, offset, labelLength);
                        songCopyright = enc.GetString(authBlock);
                        offset += labelLength + 1;
                    }                    

                    if (offset < c.chunkData.Length)
                    { 
                        labelLength = ParseFile.getSegmentLength(c.chunkData, offset, NULL_TERMINATOR);
                        authBlock = ParseFile.parseSimpleOffset(c.chunkData, offset, labelLength);
                        nsfRipper = enc.GetString(authBlock);
                        offset += labelLength + 1;
                    }                    
                }
            }
        }
        private void parseBankChunk()
        {
            foreach (ChunkStruct c in chunks)
            {
                if (ParseFile.CompareSegment(c.chunkIdentifier, 0, BANK_SIGNATURE))
                {
                    this.bankSwitchInit = c.chunkData;
                }
            }
        }

        public void GetDatFileCrc32(string pPath, ref Dictionary<string, ByteArray> pLibHash,
            ref Crc32 pChecksum, bool pUseLibHash)
        {
            pChecksum.Reset();

            pChecksum.Update(totalSongs);
            pChecksum.Update(startingSong);
            pChecksum.Update(loadAddress);
            pChecksum.Update(initAddress);
            pChecksum.Update(playAddress);
            pChecksum.Update(bankSwitchInit);
            pChecksum.Update(palNtscBits);
            pChecksum.Update(extraChipsBits);
            pChecksum.Update(data);
        }

        public byte[] GetAsciiSignature()
        {
            return ASCII_SIGNATURE;
        }

        public string GetFileExtensions()
        {
            return null;
        }

        public string GetFormatAbbreviation()
        {
            return FORMAT_ABBREVIATION;
        }

        public bool IsFileLibrary(string pPath)
        {
            return false;
        }

        public bool HasMultipleFileExtensions()
        {
            return false;
        }

        public Dictionary<string, string> GetTagHash()
        {
            return this.tagHash;
        }

        #endregion
    }
}