using System.Security.Cryptography;
using System.Text;

namespace Bitmex.NET.Authorization
{
	public interface ISignatureProvider
	{
		string CreateSignature(string secret, string message);
	}

	public class SignatureProvider : ISignatureProvider
	{
		public string CreateSignature(string secret, string message)
		{
			var secr = Encoding.UTF8.GetBytes(secret);
			var mess = Encoding.UTF8.GetBytes(message);
			var signatureBytes = Hmacsha256(secr, mess);
			return ByteArrayToString(signatureBytes);
		}

		private byte[] Hmacsha256(byte[] keyByte, byte[] messageBytes)
		{
			using (var hash = new HMACSHA256(keyByte))
			{
				return hash.ComputeHash(messageBytes);
			}
		}

		public string ByteArrayToString(byte[] ba)
		{
			var hex = new StringBuilder(ba.Length * 2);
			foreach (var b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}
	}
}
