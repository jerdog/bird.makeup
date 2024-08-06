using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BirdsiteLive.ActivityPub;
using BirdsiteLive.Common.Regexes;
using BirdsiteLive.Cryptography;
using BirdsiteLive.Domain.Factories;

namespace BirdsiteLive.Domain
{
    public interface ICryptoService
    {
        Task<string> GetUserPem(string id);
        Task<string> SignAndGetSignatureHeader(DateTime date, string actor, string host, string digest, string inbox);
        Task<string> SignString(string toSign);
        
        static abstract string ComputeSha256Hash(string data);
    }

    public class CryptoService : ICryptoService
    {
        private readonly IMagicKeyFactory _magicKeyFactory;

        #region Ctor
        public CryptoService(IMagicKeyFactory magicKeyFactory)
        {
            _magicKeyFactory = magicKeyFactory;
        }
        #endregion

        public async Task<string> GetUserPem(string id)
        {
            var key = await _magicKeyFactory.GetMagicKey();
            return key.AsPEM;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="actor">in the form of https://domain.io/actor</param>
        /// <param name="host">in the form of domain.io</param>
        /// <returns></returns>
        public async Task<string> SignAndGetSignatureHeader(DateTime date, string actor, string targethost, string digest, string inbox)
        {
            var usedInbox = "/inbox";
            if (!string.IsNullOrWhiteSpace(inbox))
                usedInbox = inbox;

            var httpDate = date.ToString("r");
            var sig64 = await SignString($"(request-target): post {usedInbox}\nhost: {targethost}\ndate: {httpDate}\ndigest: SHA-256={digest}");

            var header = "keyId=\"" + actor + "\",algorithm=\"rsa-sha256\",headers=\"(request-target) host date digest\",signature=\"" + sig64 + "\"";
            return header;
        }

        public async Task<string> SignString(string toSign) {
            var signedStringBytes = Encoding.UTF8.GetBytes(toSign);
            var key = await _magicKeyFactory.GetMagicKey();
            var signature = key.Sign(signedStringBytes);
            return Convert.ToBase64String(signature);
        }

        public static string ComputeSha256Hash(string data)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(bytes);
            }
        }
        public static SignatureValidationResult ValidateSignature(Actor remoteActor, string rawSig, string method, string path, string queryString, Dictionary<string, string> requestHeaders, string body)
        {

            //Check Date Validity
            //var date = requestHeaders["date"];
            //var d = DateTime.Parse(date).ToUniversalTime();
            //var now = DateTime.UtcNow;
            //var delta = Math.Abs((d - now).TotalSeconds);
            //if (delta > 30) return new SignatureValidationResult { SignatureIsValidated = false };
            
            
            //Check Digest
            var digest = requestHeaders["digest"];
            var digestHash = digest.Split(new [] {"SHA-256="},StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            var calculatedDigestHash = ComputeSha256Hash(body);
            if (digestHash != calculatedDigestHash) return new SignatureValidationResult { SignatureIsValidated = false };
            

            //Check Signature
            var signatures = rawSig.Split(',');
            var signature_header = new Dictionary<string, string>();
            foreach (var signature in signatures)
            {
                var m = HeaderRegexes.HeaderSignature.Match(signature);
                signature_header.Add(m.Groups[1].ToString(), m.Groups[2].ToString());
            }

            var key_id = signature_header["keyId"];
            var headers = signature_header["headers"];
            var algorithm = signature_header["algorithm"];
            var sig = Convert.FromBase64String(signature_header["signature"]);

            // Prepare Key data
            var toDecode = remoteActor.publicKey.publicKeyPem.Trim().Remove(0, remoteActor.publicKey.publicKeyPem.IndexOf('\n'));
            toDecode = toDecode.Remove(toDecode.LastIndexOf('\n')).Replace("\n", "");
            var signKey = ASN1.ToRSA(Convert.FromBase64String(toDecode));

            var toSign = new StringBuilder();
            foreach (var headerKey in headers.Split(' '))
            {
                if (headerKey == "(request-target)") toSign.Append($"(request-target): {method.ToLower()} {path}{queryString}\n");
                else toSign.Append($"{headerKey}: {string.Join(", ", requestHeaders[headerKey])}\n");
            }
            toSign.Remove(toSign.Length - 1, 1);

            // Import key
            var key = new RSACryptoServiceProvider();
            var rsaKeyInfo = key.ExportParameters(false);
            rsaKeyInfo.Modulus = Convert.FromBase64String(toDecode);
            key.ImportParameters(rsaKeyInfo);

            // Trust and Verify
            var result = signKey.VerifyData(Encoding.UTF8.GetBytes(toSign.ToString()), sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return new SignatureValidationResult()
            {
                SignatureIsValidated = result,
                User = remoteActor
            };
        }
    }
}