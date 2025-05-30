﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description:
//   XamlSerializer used to persist FigureLength structures in Baml
//

using System.Globalization;
using System.IO;
using MS.Internal;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    ///     XamlFigureLengthSerializer is used to persist a FigureLength structure in Baml files
    /// </summary>
    internal class XamlFigureLengthSerializer : XamlSerializer
    {
#region Construction

        /// <summary>
        ///     Constructor for XamlFigureLengthSerializer
        /// </summary>
        /// <remarks>
        ///     This constructor will be used under 
        ///     the following two scenarios
        ///     1. Convert a string to a custom binary representation stored in BAML
        ///     2. Convert a custom binary representation back into a FigureLength
        /// </remarks>
        private XamlFigureLengthSerializer()
        {
        }


#endregion Construction

#region Conversions

        ///<summary>
        /// Serializes this object using the passed writer.
        ///</summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method. 
        /// </remarks>
        //
        //  Format of serialized data:
        //  first byte   other bytes      format
        //  0AAAAAAA     none             Amount [0 - 127] in AAAAAAA, Pixel FigureUnitType
        //  100XXUUU     one byte         Amount in byte [0 - 255], FigureUnitType in UUU
        //  110XXUUU     two bytes        Amount in int16 , FigureUnitType in UUU
        //  101XXUUU     four bytes       Amount in int32 , FigureUnitType in UUU
        //  111XXUUU     eight bytes      Amount in double, FigureUnitType in UUU
        //
        public override bool ConvertStringToCustomBinary (
            BinaryWriter   writer,           // Writer into the baml stream
            string         stringValue)      // String to convert
        {
            ArgumentNullException.ThrowIfNull(writer);

            FigureUnitType figureUnitType;
            double   value;
            FromString(stringValue, TypeConverterHelper.InvariantEnglishUS,
                        out value, out figureUnitType);

            byte unitAndFlags = (byte)figureUnitType;
            int intAmount = (int)value;

            if ((double)intAmount == value)
            {
                //
                //  0 - 127 and Pixel
                //
                if (    intAmount <= 127 
                    &&  intAmount >= 0
                    &&  figureUnitType == FigureUnitType.Pixel  )
                {
                    writer.Write((byte)intAmount);
                }
                //
                //  unsigned byte
                //
                else if (   intAmount <= 255 
                        &&  intAmount >= 0  )
                {
                    writer.Write((byte)(0x80 | unitAndFlags));
                    writer.Write((byte)intAmount);
                }
                //
                //  signed short integer
                //
                else if (   intAmount <= 32767 
                        &&  intAmount >= -32768 )
                {
                    writer.Write((byte)(0xC0 | unitAndFlags));
                    writer.Write((Int16)intAmount);
                }
                //
                //  signed integer
                //
                else
                {
                    writer.Write((byte)(0xA0 | unitAndFlags));
                    writer.Write(intAmount);
                }
            }
            //
            //  double
            //
            else 
            {
                writer.Write((byte)(0xE0 | unitAndFlags));
                writer.Write(value);
            }

            return true;
        }
        
        /// <summary>
        ///   Convert a compact binary representation of a FigureLength into and instance
        ///   of FigureLength.  The reader must be left pointing immediately after the object 
        ///   data in the underlying stream.
        /// </summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method. 
        /// </remarks>
        public override object ConvertCustomBinaryToObject(
            BinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            FigureUnitType unitType;
            double unitValue;
            byte unitAndFlags = reader.ReadByte();

            if ((unitAndFlags & 0x80) == 0)
            {
                unitType = FigureUnitType.Pixel;
                unitValue = (double)unitAndFlags;
            }
            else
            {
                unitType = (FigureUnitType)(unitAndFlags & 0x1F);
                byte flags = (byte)(unitAndFlags & 0xE0);

                if (flags == 0x80)
                {
                    unitValue = (double)reader.ReadByte();
                }
                else if (flags == 0xC0)
                {
                    unitValue = (double)reader.ReadInt16();
                }
                else if (flags == 0xA0)
                {
                    unitValue = (double)reader.ReadInt32();
                }
                else 
                {
                    unitValue = (double)reader.ReadDouble();
                }
            }
            return new FigureLength(unitValue, unitType);
        }



        // Parse a FigureLength from a string given the CultureInfo.
        internal static void FromString(
                string       s, 
                CultureInfo  cultureInfo,
            out double       value,
            out FigureUnitType unit)
        {
            ReadOnlySpan<char> valueSpan = s.AsSpan().Trim();

            value = 0.0;
            unit = FigureUnitType.Pixel;

            int i;
            int strLenUnit = 0;
            double unitFactor = 1.0;

            //  this is where we would handle trailing whitespace on the input string.
            //  peel [unit] off the end of the string
            i = 0;

            if (valueSpan.Equals(UnitStrings[i].Name, StringComparison.OrdinalIgnoreCase))
            {
                strLenUnit = UnitStrings[i].Name.Length;
                unit = UnitStrings[i].UnitType;
            }
            else
            {
                for (i = 1; i < UnitStrings.Length; ++i)
                {
                    //  Note: this is NOT a culture specific comparison.
                    //  this is by design: we want the same unit string table to work across all cultures.
                    if (valueSpan.EndsWith(UnitStrings[i].Name, StringComparison.OrdinalIgnoreCase))
                    {
                        strLenUnit = UnitStrings[i].Name.Length;
                        unit = UnitStrings[i].UnitType;
                        break;
                    }
                }
            }

            //  we couldn't match a real unit from FigureUnitTypes.
            //  try again with a converter-only unit (a pixel equivalent).
            if (i >= UnitStrings.Length)
            {
                PixelUnit pixelUnit;
                if (PixelUnit.TryParsePixelPerInch(valueSpan, out pixelUnit)
                    || PixelUnit.TryParsePixelPerCentimeter(valueSpan, out pixelUnit)
                    || PixelUnit.TryParsePixelPerPoint(valueSpan, out pixelUnit))
                {
                    strLenUnit = pixelUnit.Name.Length;
                    unitFactor = pixelUnit.Factor;
                }
            }

            //  this is where we would handle leading whitespace on the input string.
            //  this is also where we would handle whitespace between [value] and [unit].
            //  check if we don't have a [value].  This is acceptable for certain UnitTypes.
            if (valueSpan.Length == strLenUnit && unit != FigureUnitType.Pixel)

            {
                value = 1;
            }
            //  we have a value to parse.
            else
            {
                Debug.Assert(   unit == FigureUnitType.Pixel 
                            ||  DoubleUtil.AreClose(unitFactor, 1.0)    );

                ReadOnlySpan<char> valueString = valueSpan.Slice(0, valueSpan.Length - strLenUnit);
                value = double.Parse(valueString, provider: cultureInfo) * unitFactor;
            }
        }


#endregion Conversions

#region Fields
        private struct FigureUnitTypeStringConvert
        {   
            internal FigureUnitTypeStringConvert(string name, FigureUnitType unitType)
            { 
                Name = name;
                UnitType = unitType;
            }

            internal string Name;
            internal FigureUnitType UnitType;
        };

        //  Note: keep this array in sync with the FigureUnitType enum
        private static FigureUnitTypeStringConvert[] UnitStrings =
        {
            new FigureUnitTypeStringConvert("auto",    FigureUnitType.Auto),
            new FigureUnitTypeStringConvert("px",      FigureUnitType.Pixel),
            new FigureUnitTypeStringConvert("column",  FigureUnitType.Column),
            new FigureUnitTypeStringConvert("columns", FigureUnitType.Column),
            new FigureUnitTypeStringConvert("content", FigureUnitType.Content),
            new FigureUnitTypeStringConvert("page",    FigureUnitType.Page)
        };

#endregion Fields
    }
}
