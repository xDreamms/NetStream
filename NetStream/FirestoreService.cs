//using Firebase.Auth.Requests;
//using Newtonsoft.Json;
//using QBittorrent.Client;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Drawing2D;
//using System.Drawing.Text;
//using System.Globalization;
//using System.Linq;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using System.Net.Http.Json;
//using static NBitcoin.OutputTooSmallException;
//using System.IO;
//using System.Net;
//using System.Security.Cryptography;
//using System.Windows.Media;
//using NetStream;
//using NetStream.Views;

using System.IO;
using System.Security.Cryptography;
using System.Text;
using NetStream.Views;
using Serilog;

namespace NetStream
{
    public class GetPaginatedWatchHistoryRequest
    {
        public string Email { get; set; }
        public int Page { get; set; }
    }

    public class GetPaginatedWatchHistoryResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<WatchHistory> WatchHistories { get; set; }
        public int TotalPages { get; set; }
    }

    public class FindWatchHistoryRequest
    {
        public string Email { get; set; }
        public int ShowId { get; set; }
        public ShowType ShowType { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
    }

    public class FindWatchHistoryResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public WatchHistory WatchHistory { get; set; }
    }
    public class ChangeProfileImageRequest
    {
        public string CurrentImageName { get; set; }
        public byte[] Image { get; set; }
        public string ObjectName { get; set; }
    }

    public class DownloadProfilePhotoResult
    {
        public byte[] Image { get; set; }
    }

    public class UploadProfilePhotoRequest
    {
        public byte[] Image { get; set; }
        public string objectName { get; set; }
    }

    public class DeleteCommentResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class LikeDislikeCommentResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }


    public class RegisterResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class SubPlanResult
    {
        public bool Success { get; set; }

        public string ErrorMessage { get; set; }

        public List<SubPlan> SubPlans { get; set; }
    }

    public class Encryptor
    {
        public static string Key = "LBPIGwj4CGbQB33gGFqHnapNij6GQMYw";
        public static string IV = "MX7PB6tHWB0PMNsd";
        public static string encrypt(string unencrypted)
        {
            byte[] unencryptedBytes = Encoding.UTF8.GetBytes(unencrypted);
            byte[] keyBytes = GenerateRandomBytes(32); // 256-bit key
            byte[] ivBytes = GenerateRandomBytes(16);  // 128-bit IV

            using (Aes aes256 = Aes.Create())
            {
                aes256.KeySize = 256;
                aes256.BlockSize = 128;
                aes256.Mode = CipherMode.CBC;
                aes256.Padding = PaddingMode.PKCS7;

                aes256.Key = keyBytes;
                aes256.IV = ivBytes;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes256.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(unencryptedBytes, 0, unencryptedBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    byte[] encryptedData = ms.ToArray();
                    byte[] result = new byte[keyBytes.Length + ivBytes.Length + encryptedData.Length];
                    Buffer.BlockCopy(keyBytes, 0, result, 0, keyBytes.Length);
                    Buffer.BlockCopy(ivBytes, 0, result, keyBytes.Length, ivBytes.Length);
                    Buffer.BlockCopy(encryptedData, 0, result, keyBytes.Length + ivBytes.Length, encryptedData.Length);

                    return Convert.ToBase64String(result);
                }
            }
        }

        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        public static string decrypt(string encrypted)
        {
            try
            {
                byte[] data = Convert.FromBase64String(encrypted);
                byte[] key = new byte[32];
                byte[] iv = new byte[16];
                byte[] encryptedData = new byte[data.Length - 48];

                Buffer.BlockCopy(data, 0, key, 0, 32);
                Buffer.BlockCopy(data, 32, iv, 0, 16);
                Buffer.BlockCopy(data, 48, encryptedData, 0, encryptedData.Length);

                using (Aes aes256 = Aes.Create())
                {
                    aes256.KeySize = 256;
                    aes256.BlockSize = 128;
                    aes256.Mode = CipherMode.CBC;
                    aes256.Padding = PaddingMode.PKCS7;

                    aes256.Key = key;
                    aes256.IV = iv;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes256.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedData, 0, encryptedData.Length);
                            cs.FlushFinalBlock();
                        }

                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (CryptographicException ex)
            {
                Log.Error( ex.Message);
                return "";
            }
        }
    }
    public class RootAuthResult
    {
        public Error error { get; set; }
    }

   
    
    public class SignInResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string DisplayName { get; set; }
        public string Token { get; set; }
    }

    public class SignUpResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ResetPasswordResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ChangePasswordResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ChangeUsernameResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SignInRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string EmailVerificationNeededString { get; set; }
    }

    public class SignUpRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string ResetSuccessMessage { get; set; }
    }


    public class ChangePasswordRequest
    {
        public string Email { get; set; }
        public string CorrectPassword { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string RetypedPassword { get; set; }
    }

    public class ChangeUsernameRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string NewUsername { get; set; }
    }

    public class AddCommentRequest
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public int ShowId { get; set; }
        public ShowType ShowType { get; set; }
        public string Comment { get; set; }
        public string ReplyToCommentId { get; set; }
        public string ProfilePhotoName { get; set; }
    }

    public class GetSubPlansRequest
    {
        public bool ShowTrial { get; set; }
    }

    public class IsComputerSignedUpBeforeRequest
    {
        public string Hwid { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string PlanName { get; set; }
        public string Hwid { get; set; }
        public string UserProfileImage { get; set; }
    }

    public class IsValidLoginRequest
    {
        public string Email { get; set; }
        public string MyHwid { get; set; }
        public string ErrorHwidMessage { get; set; }

        public string ErrorExpiredMessage { get; set; }

    }

    public class ChangeHwidRequest
    {
        public string Email { get; set; }
        public string MyHwid { get; set; }
        public string WaitOneDayHwidString { get; set; }
    }

    public class LikeDislikeCommentRequest
    {
        public string Email { get; set; }
        public int ShowId { get; set; }
        public ShowType ShowType { get; set; }
        public InteractionType InteractionType { get; set; }
        public string CommentId { get; set; }
    }

    public class DeleteCommentRequest
    {
        public int ShowId { get; set; }
        public ShowType ShowType { get; set; }
        public string CommentId { get; set; }
    }

    public class GetMainUserRequest
    {
        public string Email { get; set; }
    }

    public class GetCommentsRequest
    {
        public int ShowId { get; set; }
        public ShowType ShowType { get; set; }
    }

    public class DownloadProfilePhotoRequest
    {
        public string ObjectName { get; set; }
    }

    public class EncryptedJsonRequest
    {
        public string EncryptedJson { get; set; }
    }

    public class AddCommentResult
    {
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }

        public Comment Comment { get; set; }
    }
}

