using System.IO;

namespace Dalamud.Game.Network.Structures
{
    public class MarketTaxRates
    {
        /// <summary>
        /// Gets the category of this ResultDialog packet.
        /// </summary>
        public uint Category { get; private set; }

        /// <summary>
        /// Gets the tax rate in Limsa Lominsa.
        /// </summary>
        public uint LimsaLominsaTax { get; private set; }

        /// <summary>
        /// Gets the tax rate in Gridania.
        /// </summary>
        public uint GridaniaTax { get; private set; }

        /// <summary>
        /// Gets the tax rate in Ul'dah.
        /// </summary>
        public uint UldahTax { get; private set; }

        /// <summary>
        /// Gets the tax rate in Ishgard.
        /// </summary>
        public uint IshgardTax { get; private set; }

        /// <summary>
        /// Gets the tax rate in Kugane.
        /// </summary>
        public uint KuganeTax { get; private set; }

        /// <summary>
        /// Gets the tax rate in the Crystarium.
        /// </summary>
        public uint CrystariumTax { get; private set; }

        /// <summary>
        /// Gets the tax rate in Old Sharlayan.
        /// </summary>
        public uint SharlayanTax { get; private set; }

        /// <summary>
        /// Read a <see cref="MarketTaxRates"/> object from memory.
        /// </summary>
        /// <param name="message">Data to read.</param>
        /// <returns>A new <see cref="MarketTaxRates"/> object.</returns>
        public static MarketTaxRates Read(byte[] message)
        {
            using var stream = new MemoryStream(message);
            using var reader = new BinaryReader(stream);
            
            var output = new MarketTaxRates
            {
                Category = reader.ReadUInt32(),
            };

            stream.Position += 4;
            output.LimsaLominsaTax = reader.ReadUInt32();
            output.GridaniaTax = reader.ReadUInt32();
            output.UldahTax = reader.ReadUInt32();
            output.IshgardTax = reader.ReadUInt32();
            output.KuganeTax = reader.ReadUInt32();
            output.CrystariumTax = reader.ReadUInt32();
            output.SharlayanTax = reader.ReadUInt32();

            return output;
        }
    }
}