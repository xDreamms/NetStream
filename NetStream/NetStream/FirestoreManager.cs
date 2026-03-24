using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Avalonia.Platform.Storage;
using Avalonia.Controls.Shapes;
using Avalonia.Rendering.SceneGraph;
using NetStream.Annotations;
using NetStream.Views;
using Newtonsoft.Json;
using Serilog;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using System.ComponentModel.Design;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using DateTime = System.DateTime;
using System.Net.Http.Headers;
using TMDbLib.Objects.Authentication;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using Firebase.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using System.Net.Sockets;

namespace NetStream
{
    // Dil kaynaklarına erişim için yardımcı sınıf
    /*public static class ResourceProvider
    {
        // Uygulama kaynaklarından string değerini al
        public static string GetString(string key)
        {
            try
            {
                // Avalonia API'si ile uyumlu şekilde TryGetResource kullanımı
                if (Application.Current != null)
                {
                    if (Application.Current.Resources.ContainsKey(key))
                    {
                        var resource = Application.Current.Resources[key];
                        if (resource is string value)
                        {
                            return value;
                        }
                    }
                }
                return "not found"; // Kaynak bulunamazsa anahtarı döndür
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Dil kaynağı alınamadı: {key}");
                return key;
            }
        }
    }*/

    public class FirestoreManager
    {
        private static FirestoreDb database;
        private static FirebaseAuthClient firebaseAuthClient;
        private static EmailProvider emailProvider;
        private static StorageClient storageClient;
        private const string BUCKET_NAME = "netstream-1bdbe";
        private static string firebaseAuthApiKey = string.Empty;
        public static void Initialize()
        {
            try
            {
                var json = NetStreamEnvironment.GetRequiredBase64DecodedString(
                    "NETSTREAM_FIREBASE_SERVICE_ACCOUNT_JSON_BASE64",
                    "Firebase service account JSON");
                firebaseAuthApiKey = NetStreamEnvironment.GetRequiredString(
                    "NETSTREAM_FIREBASE_AUTH_API_KEY",
                    "Firebase Auth API key");

                database = new FirestoreDbBuilder() { JsonCredentials = json, ProjectId = BUCKET_NAME }.Build();
                storageClient = StorageClient.Create(GoogleCredential.FromJson(json));

                emailProvider = new EmailProvider();

                var config = new FirebaseAuthConfig
                {
                    ApiKey = firebaseAuthApiKey,
                    AuthDomain = "netstream-1bdbe.firebaseapp.com",
                    Providers = new FirebaseAuthProvider[]
                    {
                        emailProvider
                    },
                    UserRepository = new FileUserRepository("FirebaseNetStream") 
                };

                firebaseAuthClient = new FirebaseAuthClient(config);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static string BuildFirebaseOobCodeUrl()
        {
            if (string.IsNullOrWhiteSpace(firebaseAuthApiKey))
            {
                throw new InvalidOperationException("Firebase Auth API key is not configured.");
            }

            return "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=" + firebaseAuthApiKey;
        }

        public static async Task<UploadProfilePhotoResult> UploadProfilePhoto(string localPath, string objectName)
        {
            try
            {
                storageClient.UploadObject(BUCKET_NAME, objectName, "image/jpeg", new FileStream(localPath,FileMode.Open,FileAccess.Read,FileShare.Delete));
                return new UploadProfilePhotoResult()
                {
                    ErrorMessage = "",
                    Success = true
                };
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return new UploadProfilePhotoResult()
                {
                    ErrorMessage = e.Message,
                    Success = false
                };
            }
        }

        public static async Task<ChangeProfileImageResult> ChangeProfilePhoto(string localPath, string objectName)
        {
            try
            {
                await storageClient.DeleteObjectAsync(BUCKET_NAME, AppSettingsManager.appSettings.FireStoreProfilePhotoName);
                await storageClient.UploadObjectAsync(BUCKET_NAME, objectName, "image/jpeg", new FileStream(localPath,FileMode.Open,FileAccess.Read,FileShare.Delete));
                return new ChangeProfileImageResult()
                    { ErrorMessage = "", Success = true };
            }
            catch (Exception e)
            {
                if (e.Message.ToLower().Contains("no such object"))
                {
                    await storageClient.UploadObjectAsync(BUCKET_NAME, objectName, "image/jpeg", new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Delete));
                    return new ChangeProfileImageResult()
                        { ErrorMessage = e.Message, Success = true };
                }
                Log.Error(e.Message);
                return new ChangeProfileImageResult()
                    { ErrorMessage = e.Message, Success = false };
            }
        }

        public static async Task<UploadProfilePhotoResult> UploadProfilePhoto(MemoryStream stream, string objectName)
        {
            try
            {
                storageClient.UploadObject(BUCKET_NAME, objectName, "image/jpeg", stream);
                return new UploadProfilePhotoResult()
                {
                    ErrorMessage = "",
                    Success = true
                };
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return new UploadProfilePhotoResult()
                {
                    ErrorMessage = e.Message,
                    Success = false
                };
            }
        }

        public static Bitmap MyProfilePhoto = null;
        public static async Task<IImage> DownloadProfilePhoto(string objectName, bool myProfile)
        {
            try
            {
                if (myProfile && MyProfilePhoto != null)
                {
                    return MyProfilePhoto;
                }
        
                MemoryStream stream = new MemoryStream();
                await storageClient.DownloadObjectAsync(BUCKET_NAME, objectName, stream);
                stream.Seek(0, SeekOrigin.Begin);
        
                var bitmap = new Bitmap(stream);
                if (myProfile) MyProfilePhoto = bitmap;
                return bitmap;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return null;
            }
        }

        public static async Task<ResetPasswordResult> ResetPassword(string email)
        {
            try
            {
                string resetSuccessMessage = ResourceProvider.GetString("ResetSuccessMessage");
                await emailProvider.ResetEmailPasswordAsync(email);
                return new ResetPasswordResult()
                    { ErrorMessage = resetSuccessMessage, Success = true };
            }
            catch (Firebase.Auth.FirebaseAuthHttpException e)
            {
                Log.Error(e.Message);
                RootAuthResult myDeserializedClass = JsonConvert.DeserializeObject<RootAuthResult>(e.ResponseData.ToString());
                return new ResetPasswordResult()
                    { ErrorMessage = myDeserializedClass.error.message, Success = false };
            }
        }

        private static Firebase.Auth.UserCredential user;

        public static async Task<ChangePasswordResult> ChangePassword(string currentPassword, string newPassword, string retypedNewPassword)
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;
                string correctPassword = AppSettingsManager.appSettings.FireStorePassword;

                if (correctPassword == currentPassword)
                {
                    if (newPassword == retypedNewPassword)
                    {
                        if (user != null)
                        {
                            await user.User.ChangePasswordAsync(newPassword);
                            return new ChangePasswordResult() { ErrorMessage = "", Success = true };
                        }
                        else
                        {
                            user = await firebaseAuthClient.SignInWithEmailAndPasswordAsync(email, correctPassword);
                            await user.User.ChangePasswordAsync(newPassword);
                            return new ChangePasswordResult() { ErrorMessage = "", Success = true };
                        }
                    }
                    else
                    {
                        return new ChangePasswordResult() { ErrorMessage = "Passwords do not match!", Success = false };
                    }
                }
                else
                {
                    return new ChangePasswordResult() { ErrorMessage = "Current password was incorrect!", Success = false };
                }
            }
            catch (Firebase.Auth.FirebaseAuthHttpException e)
            {
                Log.Error(e.Message);
                RootAuthResult myDeserializedClass = JsonConvert.DeserializeObject<RootAuthResult>(e.ResponseData.ToString());
                return new ChangePasswordResult() { ErrorMessage = myDeserializedClass.error.message, Success = false };
            }
        }

        public static async Task<ChangeUsernameResult> ChangeUsername(string newUsername)
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;
                string password = AppSettingsManager.appSettings.FireStorePassword;

                if (user != null)
                {
                    await user.User.ChangeDisplayNameAsync(newUsername);
                    await ChangeCommentUsernames(newUsername, email);
                    return new ChangeUsernameResult() { ErrorMessage = "", Success = true };
                }
                else
                {
                    user = await firebaseAuthClient.SignInWithEmailAndPasswordAsync(email, password);
                    await user.User.ChangeDisplayNameAsync(newUsername);
                    await ChangeCommentUsernames(newUsername, email);
                    return new ChangeUsernameResult() { ErrorMessage = "", Success = true };
                }
               
            }
            catch (Firebase.Auth.FirebaseAuthHttpException e)
            {
                Log.Error(e.Message);
                RootAuthResult myDeserializedClass = JsonConvert.DeserializeObject<RootAuthResult>(e.ResponseData.ToString());
                return new ChangeUsernameResult() { ErrorMessage = myDeserializedClass.error.message, Success = false };
            }
        }

        public static async Task ChangeCommentUsernames(string newUsername, string email)
        {
            var collectionNames = new[] { "MovieComments", "TvShowComments" };

            var tasks = collectionNames.Select(async collectionName =>
            {
                var collection = database.Collection(collectionName);

                var documents = await collection.ListDocumentsAsync().ToListAsync();
                var batch = database.StartBatch();  // Batch işlemi başlatılıyor

                foreach (var doc in documents)
                {
                    var d = doc.Collection("Comments");
                    var e = await d.ListDocumentsAsync().ToListAsync();
                    foreach (var dref in e)
                    {
                        DocumentSnapshot snap = await dref.GetSnapshotAsync();
                        if (snap.Exists)
                        {
                            Dictionary<string, object> comments = snap.ToDictionary();
                            if (comments["Email"].ToString() == email)
                            {
                                batch.Update(dref, new Dictionary<string, object> { { "DisplayName", newUsername } });
                            }
                        }
                    }
                }

                await batch.CommitAsync();  // Batch işlemi tamamlanıyor
            });

            await Task.WhenAll(tasks);  // Paralel olarak tüm koleksiyonlar işleniyor
        }

        public static async Task<SignUpResult> SignUp(string email, string password, string displayName)
        {
            try
            {
                user = await firebaseAuthClient.CreateUserWithEmailAndPasswordAsync(email, password, displayName);

                VerificationPayload p = new VerificationPayload() { idToken = await user.User.GetIdTokenAsync(), requestType = "VERIFY_EMAIL" };
                string URL = BuildFirebaseOobCodeUrl();
                await RecieveData(URL, p);

                return new SignUpResult() { ErrorMessage = "", Success = true };
            }
            catch (Firebase.Auth.FirebaseAuthHttpException e)
            {
                RootAuthResult myDeserializedClass = JsonConvert.DeserializeObject<RootAuthResult>(e.ResponseData.ToString());
                return new SignUpResult()
                    { ErrorMessage = myDeserializedClass.error.message, Success = false };
            }
        }
        static NetStream.RestClient clientRest = new NetStream.RestClient();
        public static async Task<Dictionary<string, object>> RecieveData<T>(string URL, T payload)
        {
            var res = await clientRest.Post(URL, payload);


            if (res.IsSuccessStatusCode)
            {
                Stream m = await res.Content.ReadAsStreamAsync();
                Dictionary<string, object> data = await System.Text.Json.JsonSerializer.DeserializeAsync<Dictionary<string, object>>(m);
                return data;
            }
            else
            {
                string error = await res.Content.ReadAsStringAsync();
                int i = error.IndexOf("message") + 7 + 4;
                int last = error.IndexOf("\n", i) - 2;
                string subError = error.Substring(i, last - i);
                subError = subError.Replace("\\\"", "");
                throw new Exception(subError);
            }

        }

        public static async Task<SignInResult> SignIn(string email, string password)
        {
            try
            {
                user = await firebaseAuthClient.SignInWithEmailAndPasswordAsync(email, password);

                await SendLoginInfo();
                
                if (!user.User.Info.IsEmailVerified)
                {
                    return new SignInResult
                        { ErrorMessage = ResourceProvider.GetString("EmailVerificationNeededString"), Success = false };
                }
                else
                {
                    return new SignInResult
                        { ErrorMessage = "", Success = true, Token = "", DisplayName = user.User.Info.DisplayName };
                }
            }
            catch (Firebase.Auth.FirebaseAuthHttpException e)
            {
                RootAuthResult myDeserializedClass = JsonConvert.DeserializeObject<RootAuthResult>(e.ResponseData.ToString());
                return new SignInResult
                    { ErrorMessage = myDeserializedClass.error.message, Success = false };
            }
        }

        public static async Task<SendEmailVerificationResult> SendEmailVerification(string email, string password)
        {
            try
            {
                if (user == null)
                {
                    user = await firebaseAuthClient.SignInWithEmailAndPasswordAsync(email, password);
                }
                
                VerificationPayload p = new VerificationPayload() { idToken = await user.User.GetIdTokenAsync(), requestType = "VERIFY_EMAIL" };
                string URL = BuildFirebaseOobCodeUrl();
                await RecieveData(URL, p);

                return new SendEmailVerificationResult() { ErrorMessage = "", Success = true };
            }
            catch (Firebase.Auth.FirebaseAuthHttpException e)
            {
                RootAuthResult myDeserializedClass = JsonConvert.DeserializeObject<RootAuthResult>(e.ResponseData.ToString());
                return new SendEmailVerificationResult()
                    { ErrorMessage = myDeserializedClass.error.message, Success = false };
            }
        }
        static string RandomGuidString(int length) => Guid.NewGuid().ToString("N").Substring(0, length < 32 ? length : 32);
        public static async Task<AddCommentResult> AddComment(int showId, ShowType showType, string comment, string replyToCommentId, CommentPage commentPage)
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;
                string displayName = AppSettingsManager.appSettings.FireStoreDisplayName;
                string profilePhotoName = AppSettingsManager.appSettings.FireStoreProfilePhotoName;

                var doc = database.Collection(showType.ToString() + "Comments").Document(showType.ToString() + showId)
                    .Collection("Comments").Document();
                var time = await GetNistTime();
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    { "Email", email },
                    { "DisplayName", displayName },
                    { "Text", comment },
                    { "Date", (Timestamp.FromDateTime(time.ToUniversalTime())) },
                    { "Id", RandomGuidString(8) },
                    { "ReplyToCommentId", replyToCommentId },
                    { "LikeCount", 0 },
                    { "DislikeCount", 0 },
                    { "LikedBy", new List<string>() },
                    { "DisLikedBy", new List<string>() },
                    { "ProfileImage", profilePhotoName }
                };
                await doc.SetAsync(data);

                Comment com = new Comment();

                com.Email = data["Email"].ToString();
                com.DisplayName = data["DisplayName"].ToString();
                com.Text = data["Text"].ToString();
                com.Date = ((Google.Cloud.Firestore.Timestamp)data["Date"]).ToDateTime();
                com.Id = data["Id"].ToString();
                com.ReplyToCommentId = data["ReplyToCommentId"].ToString();
                com.LikeCount = Int32.Parse(data["LikeCount"].ToString());
                com.DislikeCount = Int32.Parse(data["DislikeCount"].ToString());
                com.ProfilePhoto = data["ProfileImage"].ToString();

                var likedBy = (data["LikedBy"] as List<object>);
                var dislikedBy = (data["DisLikedBy"] as List<object>);
                if (likedBy != null)
                {
                    com.LikedBy = new ObservableCollection<string>((likedBy).Cast<string>().ToList());
                }
                if (dislikedBy != null)
                {
                    com.DisLikedBy =
                        new ObservableCollection<string>((dislikedBy).Cast<string>().ToList());
                }

                // InvokeAsync ile UI Thread üzerinde işlem yap
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    commentPage.ViewboxNoCommentsFound.IsVisible = false;
                    commentPage.CommentsDisplay.IsVisible = true;
                });

                com.ProfileImage = await FirestoreManager.DownloadProfilePhoto(com.ProfilePhoto, true);
                com.RelativeDate = await GetRelativeDate(com.Date);
                if (replyToCommentId == "-1")
                {
                    if (showType == ShowType.Movie)
                    {
                        MovieComments.Add(com);
                    }
                    else
                    {
                        TvShowComments.Add(com);
                    }
                }
                else
                {
                    if (showType == ShowType.Movie)
                    {
                        foreach (var movieComment in MovieComments)
                        {
                            if (movieComment.Id == replyToCommentId)
                            {
                                if (movieComment.ReplyComments == null)
                                {
                                    movieComment.ReplyComments = new ObservableCollection<Comment>();
                                    movieComment.ReplyComments.Add(com);
                                    movieComment.ReplyCommentCount = movieComment.ReplyComments.Count + " " + ResourceProvider.GetString("Replys");
                                    commentPage.OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs(MovieComments));
                                }
                                else
                                {
                                    movieComment.ReplyComments.Add(com);
                                    movieComment.ReplyCommentCount = movieComment.ReplyComments.Count + " " + ResourceProvider.GetString("Replys");
                                    commentPage.OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs(MovieComments));
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var tvShowComment in TvShowComments)
                        {
                            if (tvShowComment.Id == replyToCommentId)
                            {
                                if (tvShowComment.ReplyComments == null)
                                {
                                    tvShowComment.ReplyComments = new ObservableCollection<Comment>();
                                    tvShowComment.ReplyComments.Add(com);
                                    tvShowComment.ReplyCommentCount = tvShowComment.ReplyComments.Count + " " + ResourceProvider.GetString("Replys");
                                    commentPage.OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs(TvShowComments));
                                }
                                else
                                {
                                    tvShowComment.ReplyComments.Add(com);
                                    tvShowComment.ReplyCommentCount = tvShowComment.ReplyComments.Count + " " + ResourceProvider.GetString("Replys");
                                    commentPage.OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs(TvShowComments));
                                }
                            }
                        }
                    }
                }

                return new AddCommentResult()
                { Success = true, ErrorMessage = "", Comment = com };
            }
            catch (Exception e)
            {
                return new AddCommentResult()
                { ErrorMessage = e.Message, Success = false };
            }
        }



        public static ObservableCollection<SubPlan> SubPlans = new ObservableCollection<SubPlan>();
        public static async Task ListenSubPlans(bool showTrial)
        {
            SubPlans.Clear();
            try
            {
                var collection = database.Collection("SubPlans");

                // Firestore query ile sadece gerekli alanları alıyoruz ve sıralama yapıyoruz
                var query = collection.OrderBy("Id").Select("Id", "PlanName", "PlanPriceString");

                var querySnapshot = await query.GetSnapshotAsync();
                foreach (var document in querySnapshot.Documents)
                {
                    var plans = document.ToDictionary();
                    SubPlan subPlan = new SubPlan
                    {
                        Id = plans["Id"].ToString(),
                        PlanName = plans["PlanName"].ToString(),
                        PlanPriceString = plans["PlanPriceString"].ToString()
                    };

                    // Trial planları filtreliyoruz
                    if (!showTrial && subPlan.PlanName.ToLower().Contains("trial"))
                    {
                        continue;
                    }

                    // Price'ı stringten decimal'e çeviriyoruz
                    subPlan.PlanPrice = decimal.Parse(subPlan.PlanPriceString.Substring(1), CultureInfo.InvariantCulture);
                    SubPlans.Add(subPlan);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }



        public static ObservableCollection<Comment> MovieComments = new ObservableCollection<Comment>();
        // ListenForChanges().Wait();

        public static ObservableCollection<Comment> TvShowComments = new ObservableCollection<Comment>();
        public static Dictionary<string, ObservableCollection<Comment>> replyComments =
            new Dictionary<string, ObservableCollection<Comment>>();

        public static async Task ListenForCommentChanges(int showId, ShowType showType, CommentPage commentPage)
        {
            MovieComments.Clear();
            TvShowComments.Clear();
            replyComments.Clear();

            var collection = database.Collection(showType.ToString() + "Comments").Document(showType.ToString() + showId).Collection("Comments");

            var query = collection.OrderBy("Date");

            var querySnapshot = await query.GetSnapshotAsync();

            var commentList = showType == ShowType.Movie ? MovieComments : TvShowComments;

            foreach (var documentSnapshot in querySnapshot.Documents)
            {
                var comments = documentSnapshot.ToDictionary();
                Comment com = new Comment
                {
                    Id = comments["Id"].ToString(),
                    Email = comments["Email"].ToString(),
                    DisplayName = comments["DisplayName"].ToString(),
                    Text = comments["Text"].ToString(),
                    Date = ((Google.Cloud.Firestore.Timestamp)comments["Date"]).ToDateTime(),
                    ReplyToCommentId = comments["ReplyToCommentId"].ToString(),
                    LikeCount = Int32.Parse(comments["LikeCount"].ToString()),
                    DislikeCount = Int32.Parse(comments["DislikeCount"].ToString()),
                    ProfilePhoto = comments["ProfileImage"].ToString()
                };

                com.LikedBy = new ObservableCollection<string>((comments["LikedBy"] as List<object>)?.Cast<string>().ToList() ?? new List<string>());
                com.DisLikedBy = new ObservableCollection<string>((comments["DisLikedBy"] as List<object>)?.Cast<string>().ToList() ?? new List<string>());

                if (com.ReplyToCommentId == "-1")
                {
                    commentList.Add(com);
                }
                else
                {
                    if (!replyComments.ContainsKey(com.ReplyToCommentId))
                    {
                        replyComments[com.ReplyToCommentId] = new ObservableCollection<Comment>();
                    }
                    replyComments[com.ReplyToCommentId].Add(com);
                }
            }

            foreach (var valueTuple in replyComments)
            {
                var mainComment = commentList.FirstOrDefault(x => x.Id == valueTuple.Key);
                if (mainComment != null)
                {
                    if (mainComment.ReplyComments == null)
                    {
                        mainComment.ReplyComments = new ObservableCollection<Comment>();
                    }
                    foreach (var VARIABLE in valueTuple.Value)
                    {
                        mainComment.ReplyComments.Add(VARIABLE);
                    }
                }
            }

            if (commentList.Any())
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    commentPage.ViewboxNoCommentsFound.IsVisible = false;
                    commentPage.CommentsDisplay.IsVisible = true;
                });
            }

            foreach (var comment in commentList)
            {
                comment.RelativeDate = await GetRelativeDate(comment.Date);
                comment.ProfileImage = await FirestoreManager.DownloadProfilePhoto(comment.ProfilePhoto, comment.Email == AppSettingsManager.appSettings.FireStoreEmail);

                if (comment.ReplyComments != null)
                {
                    comment.ReplyCommentCount = comment.ReplyComments.Count.ToString() + " " + ResourceProvider.GetString("Replys");
                    foreach (var commentReplyComment in comment.ReplyComments)
                    {
                        commentReplyComment.RelativeDate = await GetRelativeDate(commentReplyComment.Date);
                        commentReplyComment.ProfileImage = await FirestoreManager.DownloadProfilePhoto(commentReplyComment.ProfilePhoto, commentReplyComment.Email == AppSettingsManager.appSettings.FireStoreEmail);
                    }
                }
                else
                {
                    comment.ReplyComments = new ObservableCollection<Comment>();
                }
            }

            commentPage.SearchingPanel.IsVisible = false;
            var comments2 = showType == ShowType.Movie ? MovieComments : TvShowComments;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (comments2.Count == 0)
                {
                    commentPage.ViewboxNoCommentsFound.IsVisible = true;
                    commentPage.CommentsDisplay.IsVisible = false;
                }
                else
                {
                    commentPage.ViewboxNoCommentsFound.IsVisible = false;
                    commentPage.CommentsDisplay.IsVisible = true;
                }
            });
        }

        public static async Task<EditCommentResult> EditComment(int showId, ShowType showType, string commentId, string newText)
        {
            try
            {
                var docRef = database.Collection(showType.ToString() + "Comments")
                    .Document(showType.ToString() + showId)
                    .Collection("Comments")
                    .WhereEqualTo("Id", commentId)
                    .WhereEqualTo("Email", AppSettingsManager.appSettings.FireStoreEmail);

                var querySnapshot = await docRef.GetSnapshotAsync();

                if (querySnapshot.Count > 0)
                {
                    var documentSnapshot = querySnapshot.Documents.First();
                    if (documentSnapshot.Exists)
                    {
                        var currentTime = await GetNistTime();
                        await documentSnapshot.Reference.UpdateAsync("Text", newText);
                        await documentSnapshot.Reference.UpdateAsync("Date", Timestamp.FromDateTime(currentTime.ToUniversalTime()));

                        foreach (var c in showType == ShowType.Movie ? MovieComments : TvShowComments)
                        {
                            if (c.Id == commentId)
                            {
                                c.Text = newText;
                                c.Date = currentTime.ToUniversalTime();
                                c.RelativeDate = await GetRelativeDate(c.Date);
                            }

                            foreach (var cReplyComment in c.ReplyComments)
                            {
                                if (cReplyComment.Id == commentId)
                                {
                                    cReplyComment.Text = newText;
                                    cReplyComment.Date = currentTime.ToUniversalTime();
                                    cReplyComment.RelativeDate =await GetRelativeDate(cReplyComment.Date);
                                }
                            }
                        }
                        return new EditCommentResult()
                            { ErrorMessage = "", Success = true };
                    }
                }

                return new EditCommentResult()
                    { ErrorMessage = "Couldnt find comment", Success = false };
            }
            catch (Exception e)
            {
                return new EditCommentResult()
                    { ErrorMessage = e.Message, Success = false };
            }
        }

        public static async Task<bool> IsComputerSignedUpBefore()
        {
            try
            {
                string hwid = "";

                var query = database.Collection("Users").WhereEqualTo("Hwid", hwid);

                var querySnapshot = await query.GetSnapshotAsync();
                return querySnapshot.Documents.Any();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }
        }

        public static async Task SendLoginInfo()
        {
            string email = AppSettingsManager.appSettings.FireStoreEmail;
            var querySnapshot = await database.Collection("Users").WhereEqualTo("Email", email).GetSnapshotAsync();
            if (querySnapshot.Documents.Any())
            {
                var documentSnapshot = querySnapshot.Documents.First();
                var documentReference = documentSnapshot.Reference; 
                var snap = await documentReference.GetSnapshotAsync();
                if (snap.Exists)
                {
                    Dictionary<string, object> users = snap.ToDictionary();
                    if (users["Email"].ToString() == email)
                    {
                        string LocalipAddress = await Task.Run(() => QRCodeGeneratorControl.GetLocalIPv4Address());
                        string LocalwebsocketUrl = $"ws://{LocalipAddress}:4649/NetStreamSocket";
                        await documentReference.UpdateAsync("connectionLocal",LocalwebsocketUrl);
                    }
                }
            }
            
        }

        public static async Task<RegisterResult> Register(SubPlan subPlan)
        {
            try
            {
                string planName = subPlan.PlanName;
                string email = AppSettingsManager.appSettings.FireStoreEmail;
                string hwid = "";
                string userProfileImage = AppSettingsManager.appSettings.FireStoreProfilePhotoName;

                // Plan süresi hesaplama
                string days = planName.ToLower().Contains("lifetime") ? "36500" : Regex.Match(planName, @"\d+").Value;
                days = (Int32.Parse(days) + 1).ToString();

                var querySnapshot = await database.Collection("Users").WhereEqualTo("Email", email).GetSnapshotAsync();

                // Eğer kullanıcı zaten kayıtlıysa, süresini uzat
                if (querySnapshot.Documents.Any())
                {
                    var documentSnapshot = querySnapshot.Documents.First();
                    var documentReference = documentSnapshot.Reference;  // DocumentSnapshot'tan Reference alıyoruz
                    return await ExtendSubTime(days, documentReference, email);
                }
                else
                {
                    // Yeni kullanıcı kaydı oluştur
                    var doc = database.Collection("Users").Document();
                    var time = await GetNistTime();
                    var newTime = Timestamp.FromDateTime(time.AddDays(Int32.Parse(days)).ToUniversalTime());
                    
                    var userInfo = await GetUserInfoAsync();
                    string LocalipAddress = await Task.Run(() => QRCodeGeneratorControl.GetLocalIPv4Address());
                    string localwebsocketUrl = $"ws://{LocalipAddress}:4649/NetStreamSocket";
                    var data = new Dictionary<string, object>()
                    {
                        { "Email", email },
                        { "Hwid", hwid },
                        { "ExpiryDate", newTime },
                        { "LastHwidChange", Timestamp.FromDateTime(time.ToUniversalTime()) },
                        { "UserProfileImage", userProfileImage },
                        {"IP", userInfo.ip},
                        {"hostname",userInfo.hostname},
                        {"city",userInfo.city},
                        {"region",userInfo.region},
                        {"country",userInfo.country},
                        {"loc",userInfo.loc},
                        {"org",userInfo.org},
                        {"postal",userInfo.postal},
                        {"timezone",userInfo.timezone},
                        {"connectionLocal",localwebsocketUrl},
                        {"tmdbUsername", String.Empty},
                        {"tmdbPassword", String.Empty}
                    };

                    await doc.SetAsync(data);
                    ExpiryDate = newTime.ToDateTime();
                    return new RegisterResult()
                    {
                        ErrorMessage = "",
                        Success = true,
                        ExpiryDate = newTime.ToDateTime()
                    };
                }
            }
            catch (Exception e)
            {
                return new RegisterResult()
                {
                    ErrorMessage = e.Message,
                    Success = false,
                    ExpiryDate = DateTime.MinValue
                };
            }
        }
        static async Task<UserInfo> GetUserInfoAsync()
        {
            string url = "https://ipinfo.io/json";
            using (HttpClient client = new HttpClient())
            {
                string result = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<UserInfo>(result);
            }
        }
        public static async Task<RegisterResult> ExtendSubTime(string days, Google.Cloud.Firestore.DocumentReference doc, string email)
        {
            try
            {
                // Belgeyi al
                var snap = await doc.GetSnapshotAsync();
                if (snap.Exists)
                {
                    Dictionary<string, object> users = snap.ToDictionary();
                    if (users["Email"].ToString() == email)
                    {
                        var expiry_date = ((Timestamp)users["ExpiryDate"]).ToDateTime();
                        var current_date = await GetNistTime();
                        current_date = current_date.ToUniversalTime();
                        // Geçerlilik tarihini hesapla ve güncelle
                        var newDate = expiry_date < current_date
                            ? (Timestamp.FromDateTime(current_date.AddDays(Int32.Parse(days)).ToUniversalTime()))
                            : (Timestamp.FromDateTime(expiry_date.AddDays(Int32.Parse(days)).ToUniversalTime()));

                        await doc.UpdateAsync("ExpiryDate", newDate);
                        ExpiryDate = newDate.ToDateTime();
                        return new RegisterResult()
                        {
                            ErrorMessage = "",
                            Success = true,
                            ExpiryDate = newDate.ToDateTime()
                        };
                    }
                }
            }
            catch (Exception e)
            {
                return new RegisterResult()
                {
                    ErrorMessage = e.Message,
                    Success = false,
                    ExpiryDate = DateTime.MinValue
                };
            }

            return new RegisterResult()
            {
                ErrorMessage = "Error",
                Success = false,
                ExpiryDate = DateTime.MinValue
            };
        }


        public static string TmdbUsername;
        public static string TMdbpassword;
        public static DateTime ExpiryDate;
        public static DateTime CurrentTime;
        public static async Task<LoginResult> IsValidLogin()
        {
            string email = AppSettingsManager.appSettings.FireStoreEmail;
            var collection = database.Collection("Users");
            var querySnapshot = await collection.WhereEqualTo("Email", email).GetSnapshotAsync();

            if (querySnapshot.Documents.Any())
            {
                var snap = querySnapshot.Documents.First();
                Dictionary<string, object> users = snap.ToDictionary();

                // Check for TMDB credentials and auto-login if not already logged in
                if (
                    users.ContainsKey("tmdbUsername") && users.ContainsKey("tmdbPassword"))
                {
                    string tmdbUser = users["tmdbUsername"].ToString();
                    string tmdbPass = users["tmdbPassword"].ToString();

                    if (!string.IsNullOrEmpty(tmdbUser) && !string.IsNullOrEmpty(tmdbPass))
                    {
                        await Service.client.GetConfigAsync();
                        var loginResult = await Service.Login(tmdbUser,
                            tmdbPass);
                        if (loginResult)
                        {
                            Log.Information("Logged in to TMDB account");
                        }
                        // AppSettingsManager.appSettings.TmdbUsername = tmdbUser;
                        // AppSettingsManager.appSettings.TmdbPassword = tmdbPass;
                        // AppSettingsManager.SaveAppSettings();
                        TmdbUsername = tmdbUser;
                        TMdbpassword = tmdbPass;
                    }
                }
            }
            return new LoginResult()
            {
                Success = true,
                ErrorMessage = "",
                ErrorType = ErrorType.NoError,
                ExpiryTime = DateTime.MaxValue,
                CurrentTime = DateTime.Now
            };
            // try
            // {
            //     string email = AppSettingsManager.appSettings.FireStoreEmail;
            //     var collection = database.Collection("Users");
            //     var querySnapshot = await collection.WhereEqualTo("Email", email).GetSnapshotAsync();
            //
            //     if (querySnapshot.Documents.Any())
            //     {
            //         var snap = querySnapshot.Documents.First();
            //         Dictionary<string, object> users = snap.ToDictionary();
            //
            //         // Check for TMDB credentials and auto-login if not already logged in
            //         if (string.IsNullOrEmpty(AppSettingsManager.appSettings.TmdbUsername) && 
            //             users.ContainsKey("tmdbUsername") && users.ContainsKey("tmdbPassword"))
            //         {
            //             string tmdbUser = users["tmdbUsername"].ToString();
            //             string tmdbPass = users["tmdbPassword"].ToString();
            //
            //             if (!string.IsNullOrEmpty(tmdbUser) && !string.IsNullOrEmpty(tmdbPass))
            //             {
            //                 await Service.Login(tmdbUser, tmdbPass);
            //             }
            //         }
            //     }
            // }
            // catch (Exception e)
            // {
            //     Log.Error("Error in IsValidLogin TMDB check: " + e.Message);
            // }
            
            //try
            //{
            //    string email = AppSettingsManager.appSettings.FireStoreEmail;
            //    string myHwid = HWIDGenerator.GetHWID();
            //    string expiredError = App.Current.Resources["SubExpired"].ToString();
            //    string hwidError = App.Current.Resources["MachineNotRecognized"].ToString();

            //    var collection = database.Collection("Users");

            //    // Email'e göre direkt sorgu yapalım
            //    var querySnapshot = await collection.WhereEqualTo("Email", email).GetSnapshotAsync();

            //    if (querySnapshot.Documents.Any())
            //    {
            //        var snap = querySnapshot.Documents.First();
            //        Dictionary<string, object> users = snap.ToDictionary();

            //        var expiryDate = ((Google.Cloud.Firestore.Timestamp)users["ExpiryDate"]).ToDateTime();
            //        var hwid = users["Hwid"].ToString();
            //        var currentTime = await GetNistTime();
            //        currentTime = currentTime.ToUniversalTime();

            //        if (expiryDate > currentTime && hwid == myHwid)
            //        {
            //            ExpiryDate = expiryDate;
            //            CurrentTime = currentTime;
            //            return new LoginResult()
            //            {
            //                Success = true,
            //                ErrorMessage = "",
            //                ErrorType = ErrorType.NoError,
            //                ExpiryTime = expiryDate,
            //                CurrentTime = currentTime
            //            };
            //        }

            //        if (expiryDate < currentTime)
            //        {
            //            return new LoginResult()
            //            {
            //                ErrorMessage = expiredError,
            //                ErrorType = ErrorType.Expired,
            //                Success = false,
            //                CurrentTime = currentTime,
            //                ExpiryTime = expiryDate
            //            };
            //        }

            //        if (hwid != myHwid)
            //        {
            //            return new LoginResult()
            //            {
            //                ErrorMessage = hwidError,
            //                ErrorType = ErrorType.Hwid,
            //                Success = false,
            //                ExpiryTime = expiryDate,
            //                CurrentTime = currentTime
            //            };
            //        }
            //    }

            //    var time = await GetNistTime();
            //    // Eğer kullanıcı bulunamazsa
            //    return new LoginResult()
            //    {
            //        ErrorMessage = "Couldn't find the user!",
            //        ErrorType = ErrorType.UserNotFound,
            //        Success = false,
            //        CurrentTime = time.ToUniversalTime(),
            //        ExpiryTime = DateTime.MinValue
            //    };
            //}
            //catch (Exception e)
            //{
            //    var time = await GetNistTime();
            //    // Hata durumu
            //    return new LoginResult()
            //    {
            //        ErrorMessage = e.Message,
            //        ErrorType = ErrorType.UserNotFound,
            //        Success = false,
            //        CurrentTime = time.ToUniversalTime(),
            //        ExpiryTime = DateTime.MinValue
            //    };
            //}
        }



        public static async Task<ChangeHwidResult> ChangeHwid()
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;
                string myHwid = "";
                string waitOneDayHwidString = ResourceProvider.GetString("WaitOneDayHwid");

                var collection = database.Collection("Users");

                // Kullanıcıyı email ile doğrudan sorgula
                var querySnapshot = await collection.WhereEqualTo("Email", email).GetSnapshotAsync();

                if (!querySnapshot.Documents.Any())
                {
                    return new ChangeHwidResult()
                    {
                        ChangeHwidErrorType = ChangeHwidErrorType.UserNotFound,
                        ErrorMessage = "Couldn't find the user!",
                        Success = false
                    };
                }

                var snap = querySnapshot.Documents.First();
                var users = snap.ToDictionary();

                var lastchange = ((Google.Cloud.Firestore.Timestamp)users["LastHwidChange"]).ToDateTime();
                var time = await GetNistTime();
                if (time.ToUniversalTime() - lastchange >= TimeSpan.FromDays(1))
                {
                    // Hwid değişikliğini güncelle
                    await snap.Reference.UpdateAsync("Hwid", myHwid);
                    await snap.Reference.UpdateAsync("LastHwidChange",
                        Google.Cloud.Firestore.Timestamp.FromDateTime(time.ToUniversalTime()));

                    return new ChangeHwidResult()
                    {
                        ChangeHwidErrorType = ChangeHwidErrorType.NoError,
                        ErrorMessage = "",
                        Success = true
                    };
                }
                else
                {
                    return new ChangeHwidResult()
                    {
                        ChangeHwidErrorType = ChangeHwidErrorType.Time,
                        ErrorMessage = waitOneDayHwidString,
                        Success = false
                    };
                }
            }
            catch (Exception e)
            {
                return new ChangeHwidResult()
                {
                    ChangeHwidErrorType = ChangeHwidErrorType.UserNotFound,
                    ErrorMessage = e.Message,
                    Success = false
                };
            }
        }

        public static async Task UpdateTmdbCredentials(string username, string password)
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;
                var collection = database.Collection("Users");
                var querySnapshot = await collection.WhereEqualTo("Email", email).GetSnapshotAsync();

                if (querySnapshot.Documents.Any())
                {
                    var snap = querySnapshot.Documents.First();
                    await snap.Reference.UpdateAsync("tmdbUsername", username);
                    await snap.Reference.UpdateAsync("tmdbPassword", password);
                }
            }
            catch(Exception e){
                Log.Error("Error updating TMDB credentials: " + e.Message);
            }
        }

        public static async Task<LikeDislikeCommentResult> LikeDislikeComment(int showId, ShowType showType, InteractionType interactionType, string commentId, CommentPage commentPage)
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;

                var collection = database.Collection($"{showType}Comments")
                                          .Document($"{showType}{showId}")
                                          .Collection("Comments");

                // Sadece ilgili yorumları almak için sorgu kullanarak performansı artır
                var querySnapshot = await collection
                    .WhereEqualTo("Id", commentId)
                    .WhereNotEqualTo("Email", email)
                    .GetSnapshotAsync();

                var doc = querySnapshot.Documents.FirstOrDefault();
                if (doc != null)
                {
                    var comments = doc.ToDictionary();
                    var likedBy = comments["LikedBy"] as List<object>;
                    var dislikedBy = comments["DisLikedBy"] as List<object>;

                    if (interactionType == InteractionType.Like)
                    {
                        if (dislikedBy?.Contains(email) == true)
                        {
                            await doc.Reference.UpdateAsync("DislikeCount", FieldValue.Increment(-1));
                            dislikedBy.Remove(email);
                            await doc.Reference.UpdateAsync("DisLikedBy", dislikedBy);
                        }

                        if (likedBy == null)
                        {
                            likedBy = new List<object> { email };
                            await doc.Reference.UpdateAsync("LikeCount", FieldValue.Increment(1));
                            await doc.Reference.UpdateAsync("LikedBy", likedBy);
                        }
                        else
                        {
                            if (likedBy.Contains(email))
                            {
                                await doc.Reference.UpdateAsync("LikeCount", FieldValue.Increment(-1));
                                likedBy.Remove(email);
                            }
                            else
                            {
                                await doc.Reference.UpdateAsync("LikeCount", FieldValue.Increment(1));
                                likedBy.Add(email);
                            }
                            await doc.Reference.UpdateAsync("LikedBy", likedBy);
                        }
                    }
                    else // InteractionType.Dislike
                    {
                        if (likedBy?.Contains(email) == true)
                        {
                            await doc.Reference.UpdateAsync("LikeCount", FieldValue.Increment(-1));
                            likedBy.Remove(email);
                            await doc.Reference.UpdateAsync("LikedBy", likedBy);
                        }

                        if (dislikedBy == null)
                        {
                            dislikedBy = new List<object> { email };
                            await doc.Reference.UpdateAsync("DislikeCount", FieldValue.Increment(1));
                            await doc.Reference.UpdateAsync("DisLikedBy", dislikedBy);
                        }
                        else
                        {
                            if (dislikedBy.Contains(email))
                            {
                                await doc.Reference.UpdateAsync("DislikeCount", FieldValue.Increment(-1));
                                dislikedBy.Remove(email);
                            }
                            else
                            {
                                await doc.Reference.UpdateAsync("DislikeCount", FieldValue.Increment(1));
                                dislikedBy.Add(email);
                            }
                            await doc.Reference.UpdateAsync("DisLikedBy", dislikedBy);
                        }
                    }

                    foreach (var movieComment in (showType == ShowType.Movie ? MovieComments : TvShowComments))
                    {
                        if (movieComment.Id == commentId)
                        {
                            if (interactionType == InteractionType.Like)
                            {
                                if (!(movieComment.LikedBy.Any(x =>
                                        x == AppSettingsManager.appSettings.FireStoreEmail)))
                                {
                                    movieComment.LikedBy.Add(AppSettingsManager.appSettings.FireStoreEmail);
                                    movieComment.LikeCount++;
                                    if (movieComment.DisLikedBy.Any(x =>
                                            x == AppSettingsManager.appSettings.FireStoreEmail))
                                    {
                                        movieComment.DisLikedBy.Remove(AppSettingsManager.appSettings.FireStoreEmail);
                                        movieComment.DislikeCount--;
                                    }
                                    commentPage.OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs((showType == ShowType.Movie ? MovieComments : TvShowComments)));
                                }
                                else
                                {
                                    movieComment.LikedBy.Remove(AppSettingsManager.appSettings.FireStoreEmail);
                                    movieComment.LikeCount--;
                                    commentPage.OnItemLoadingFinished(
                                        new OnItemLoadingFinishedEventArgs((showType == ShowType.Movie
                                            ? MovieComments
                                            : TvShowComments)));
                                }
                            }
                            else
                            {
                                if (!(movieComment.DisLikedBy.Any(x =>
                                        x == AppSettingsManager.appSettings.FireStoreEmail)))
                                {
                                    movieComment.DisLikedBy.Add(AppSettingsManager.appSettings.FireStoreEmail);
                                    movieComment.DislikeCount++;
                                    if (movieComment.LikedBy.Any(x =>
                                            x == AppSettingsManager.appSettings.FireStoreEmail))
                                    {
                                        movieComment.LikedBy.Remove(AppSettingsManager.appSettings.FireStoreEmail);
                                        movieComment.LikeCount--;
                                    }

                                    commentPage.OnItemLoadingFinished(
                                        new OnItemLoadingFinishedEventArgs((showType == ShowType.Movie
                                            ? MovieComments
                                            : TvShowComments)));
                                }
                                else
                                {
                                    movieComment.DisLikedBy.Remove(AppSettingsManager.appSettings.FireStoreEmail);
                                    movieComment.DislikeCount--;
                                    commentPage.OnItemLoadingFinished(
                                        new OnItemLoadingFinishedEventArgs((showType == ShowType.Movie
                                            ? MovieComments
                                            : TvShowComments)));
                                }
                            }
                        }

                        foreach (var movieCommentReplyComment in movieComment.ReplyComments)
                        {
                            if (movieCommentReplyComment.Id == commentId)
                            {
                                if (interactionType == InteractionType.Like)
                                {
                                    if (!(movieCommentReplyComment.LikedBy.Any(x =>
                                            x == AppSettingsManager.appSettings.FireStoreEmail)))
                                    {
                                        movieCommentReplyComment.LikedBy.Add(AppSettingsManager.appSettings
                                            .FireStoreEmail);
                                        movieCommentReplyComment.LikeCount++;
                                        if (movieCommentReplyComment.DisLikedBy.Any(x =>
                                                x == AppSettingsManager.appSettings.FireStoreEmail))
                                        {
                                            movieCommentReplyComment.DisLikedBy.Remove(AppSettingsManager.appSettings.FireStoreEmail);
                                            movieCommentReplyComment.DislikeCount--;
                                        }

                                        commentPage.OnItemLoadingFinished(
                                            new OnItemLoadingFinishedEventArgs((showType == ShowType.Movie
                                                ? MovieComments
                                                : TvShowComments)));
                                    }
                                    else
                                    {
                                        movieCommentReplyComment.LikedBy.Remove(AppSettingsManager.appSettings.FireStoreEmail);
                                        movieCommentReplyComment.LikeCount--;
                                        commentPage.OnItemLoadingFinished(
                                            new OnItemLoadingFinishedEventArgs((showType == ShowType.Movie
                                                ? MovieComments
                                                : TvShowComments)));
                                    }
                                }
                                else
                                {
                                    if (!(movieCommentReplyComment.DisLikedBy.Any(x =>
                                            x == AppSettingsManager.appSettings.FireStoreEmail)))
                                    {
                                        movieCommentReplyComment.DisLikedBy.Add(AppSettingsManager.appSettings
                                            .FireStoreEmail);
                                        movieCommentReplyComment.DislikeCount++;
                                        if (movieCommentReplyComment.LikedBy.Any(x =>
                                                x == AppSettingsManager.appSettings.FireStoreEmail))
                                        {
                                            movieCommentReplyComment.LikedBy.Remove(AppSettingsManager.appSettings
                                                .FireStoreEmail);
                                            movieCommentReplyComment.LikeCount--;
                                        }

                                        commentPage.OnItemLoadingFinished(
                                            new OnItemLoadingFinishedEventArgs((showType == ShowType.Movie
                                                ? MovieComments
                                                : TvShowComments)));
                                    }
                                    else
                                    {
                                        movieCommentReplyComment.DisLikedBy.Remove(AppSettingsManager.appSettings.FireStoreEmail);
                                        movieCommentReplyComment.DislikeCount--;
                                        commentPage.OnItemLoadingFinished(
                                            new OnItemLoadingFinishedEventArgs((showType == ShowType.Movie
                                                ? MovieComments
                                                : TvShowComments)));
                                    }
                                }
                            }
                        }
                    }

                    return new LikeDislikeCommentResult()
                    {
                        ErrorMessage = "",
                        Success = true
                    };
                }

                return new LikeDislikeCommentResult()
                {
                    ErrorMessage = "Comment not found or already interacted with.",
                    Success = false
                };
            }
            catch (Exception e)
            {
                return new LikeDislikeCommentResult()
                {
                    ErrorMessage = e.Message,
                    Success = false
                };
            }
        }

        public static async Task<DeleteCommentResult> DeleteComment(int showId, ShowType showType, string commentId, CommentPage commentPage)
        {
            try
            {
                var collection = database.Collection(showType.ToString() + "Comments").Document(showType.ToString() + showId)
                    .Collection("Comments");

                // Retrieve all document references once
                var documentRefs = await collection.ListDocumentsAsync().ToListAsync();

                // Find documents where the commentId matches
                var documentsToDelete = documentRefs.Where(docRef =>
                {
                    var snap = docRef.GetSnapshotAsync().Result;
                    var comments = snap.ToDictionary();
                    return comments["Id"].ToString() == commentId || comments["ReplyToCommentId"].ToString() == commentId;
                }).ToList();

                // Delete matched documents
                var deleteTasks = documentsToDelete.Select(docRef => docRef.DeleteAsync()).ToList();

                // Wait for all delete operations to complete
                await Task.WhenAll(deleteTasks);

                foreach (var c in showType == ShowType.Movie ? MovieComments : TvShowComments)
                {
                    if (c.Id == commentId)
                    {
                        var remove =
                            (showType == ShowType.Movie ? MovieComments : TvShowComments).FirstOrDefault(x =>
                                x.Id == c.Id);
                        (showType == ShowType.Movie ? MovieComments : TvShowComments).Remove(remove);
                        commentPage.OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs(showType == ShowType.Movie ? MovieComments : TvShowComments));
                    }

                    if (c.ReplyComments != null)
                    {
                        foreach (var cReplyComment in c.ReplyComments)
                        {
                            if (cReplyComment.Id == commentId)
                            {
                                var remove = (c.ReplyComments).FirstOrDefault(x =>
                                    x.Id == commentId);
                                (c.ReplyComments).Remove(remove);
                                c.ReplyCommentCount = c.ReplyComments.Count + " " + ResourceProvider.GetString("Replys");
                                commentPage.OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs(showType == ShowType.Movie ? MovieComments : TvShowComments));
                            }
                        }
                    }
                }

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var comments = showType == ShowType.Movie ? MovieComments : TvShowComments;
                    if (comments.Count == 0)
                    {
                        commentPage.ViewboxNoCommentsFound.IsVisible = true;
                    }
                });

                return new DeleteCommentResult()
                {
                    ErrorMessage = "",
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new DeleteCommentResult()
                    { ErrorMessage = e.Message, Success = false };
            }
        }

        public static async Task<string> GetRelativeDate(DateTime dt)
        {
            var time = await GetNistTime();
            var ts = new TimeSpan(time.ToUniversalTime().Ticks - dt.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 60)
            {
                return ts.Seconds == 1 ? ResourceProvider.GetString("OneSecondAgo") : ts.Seconds + " " + ResourceProvider.GetString("SecondsAgo");
            }
            if (delta < 60 * 2)
            {
                return ResourceProvider.GetString("AMinuteAgo");
            }
            if (delta < 45 * 60)
            {
                return ts.Minutes + " " + ResourceProvider.GetString("MinutesAgo");
            }
            if (delta < 90 * 60)
            {
                return ResourceProvider.GetString("AnHourAgo");
            }
            if (delta < 24 * 60 * 60)
            {
                return ts.Hours + " " + ResourceProvider.GetString("HoursAgo");
            }
            if (delta < 48 * 60 * 60)
            {
                return ResourceProvider.GetString("Yesterday");
            }
            if (delta < 30 * 24 * 60 * 60)
            {
                return ts.Days + " " + ResourceProvider.GetString("DaysAgo");
            }
            if (delta < 12 * 30 * 24 * 60 * 60)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? ResourceProvider.GetString("OneMonthAgo") : months + " " + ResourceProvider.GetString("MonthsAgo");
            }
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? ResourceProvider.GetString("OneYearAgo") : years + " " + ResourceProvider.GetString("YearsAgo");
        }

        public static string GetRelativeDate(DateTime dt, DateTime currentTime)
        {
            var ts = new TimeSpan(currentTime.Ticks - dt.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 60)
            {
                return ts.Seconds == 1 ? ResourceProvider.GetString("OneSecondAgo") : ts.Seconds + " " + ResourceProvider.GetString("SecondsAgo");
            }
            if (delta < 60 * 2)
            {
                return ResourceProvider.GetString("AMinuteAgo");
            }
            if (delta < 45 * 60)
            {
                return ts.Minutes + " " + ResourceProvider.GetString("MinutesAgo");
            }
            if (delta < 90 * 60)
            {
                return ResourceProvider.GetString("AnHourAgo");
            }
            if (delta < 24 * 60 * 60)
            {
                return ts.Hours + " " + ResourceProvider.GetString("HoursAgo");
            }
            if (delta < 48 * 60 * 60)
            {
                return ResourceProvider.GetString("Yesterday");
            }
            if (delta < 30 * 24 * 60 * 60)
            {
                return ts.Days + " " + ResourceProvider.GetString("DaysAgo");
            }
            if (delta < 12 * 30 * 24 * 60 * 60)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? ResourceProvider.GetString("OneMonthAgo") : months + " " + ResourceProvider.GetString("MonthsAgo");
            }
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? ResourceProvider.GetString("OneYearAgo") : years + " " + ResourceProvider.GetString("YearsAgo");
        }

        public static SolidColorBrush ExpiryDateTextColor;

        public static async Task<string> GetRelativeSubEndTime(DateTime subEndTime)
        {
            var time = await GetNistTime();
            var currentTime = time.ToUniversalTime();
            if (subEndTime < currentTime)
            {
                ExpiryDateTextColor = new SolidColorBrush(Colors.Red);
                return ResourceProvider.GetString("Expired");
            }
            else
            {
                var ts = new TimeSpan(subEndTime.Ticks - time.ToUniversalTime().Ticks);
                double delta = Math.Abs(ts.TotalSeconds);

                if (delta < 60)
                {
                    ExpiryDateTextColor = new SolidColorBrush(Colors.Red);
                    return ts.Seconds == 1 ? ResourceProvider.GetString("oneSecond") : ts.Seconds + " " + ResourceProvider.GetString("seconds");
                }
                if (delta < 60 * 2)
                {
                    ExpiryDateTextColor = new SolidColorBrush(Colors.Red);
                    return ResourceProvider.GetString("oneMinute");
                }
                if (delta < 45 * 60)
                {
                    ExpiryDateTextColor = new SolidColorBrush(Colors.Red);
                    return ts.Minutes + " " + ResourceProvider.GetString("minutes");
                }
                if (delta < 90 * 60)
                {
                    ExpiryDateTextColor = new SolidColorBrush(Colors.Red);
                    return ResourceProvider.GetString("oneHour");
                }
                if (delta < 24 * 60 * 60)
                {
                    ExpiryDateTextColor = new SolidColorBrush(Colors.Red);
                    return ts.Hours + " " + ResourceProvider.GetString("hours");
                }
                if (delta < 48 * 60 * 60)
                {
                    ExpiryDateTextColor = new SolidColorBrush(Colors.Red);
                    return ResourceProvider.GetString("oneDay");
                }
                if (delta < 30 * 24 * 60 * 60)
                {
                    if (ts.Days <= 3)
                    {
                        ExpiryDateTextColor = new SolidColorBrush(Colors.Red);
                    }
                    else if (ts.Days > 3 && ts.Days <= 10)
                    {
                        ExpiryDateTextColor = new SolidColorBrush(Colors.Yellow);
                    }
                    else
                    {
                        ExpiryDateTextColor = new SolidColorBrush(Colors.Green);
                    }
                    return ts.Days + " " + ResourceProvider.GetString("days");
                }
                if (delta < 12 * 30 * 24 * 60 * 60)
                {
                    ExpiryDateTextColor = new SolidColorBrush(Colors.Green);
                    int months = Convert.ToInt32(Math.Ceiling((double)ts.Days / 30));
                    return months <= 1 ? ResourceProvider.GetString("oneMonth") : months + " " + ResourceProvider.GetString("months");
                }
                ExpiryDateTextColor = new SolidColorBrush(Colors.Green);
                int years = Convert.ToInt32(Math.Ceiling((double)ts.Days / 365));
                return years <= 1 ? ResourceProvider.GetString("oneYear") : years + " " + ResourceProvider.GetString("years");
            }
        }

        private static List<string> _BackgroundColours = new List<string> { "339966", "3366CC", "CC33FF", "FF5050" };

        public static MemoryStream GenerateCircle(string firstName, string lastName)
        {
            try
            {
                // Karakter dizisini oluştur (ad-soyad ilk harfleri)
                string avatarString = "";
                if (String.IsNullOrWhiteSpace(lastName))
                {
                    avatarString = string.Format("{0}", firstName[0]).ToUpper();
                }
                else
                {
                    avatarString = string.Format("{0}{1}", firstName[0], lastName[0]).ToUpper();
                }

                // Rastgele renk seç
                var randomIndex = new Random().Next(0, _BackgroundColours.Count - 1);
                var bgColour = _BackgroundColours[randomIndex];
                
                // Avalonia yazı tipi ve renk ayarları
                var foregroundColor = Colors.WhiteSmoke;
                var backgroundColor = Color.Parse("#" + bgColour);
                
                // Bitmap oluştur ve stream'e kaydet
                int size = 192;
                var renderTarget = new RenderTargetBitmap(new Avalonia.PixelSize(size, size));
                
                using (var drawingContext = renderTarget.CreateDrawingContext(true))
                {
                    // Arkaplan dairesini çiz
                    var circle = new Avalonia.Controls.Shapes.Ellipse
                    {
                        Width = size,
                        Height = size,
                        Fill = new SolidColorBrush(backgroundColor)
                    };
                    
                    // Yazıyı merkeze yerleştir
                    var textBlock = new Avalonia.Controls.TextBlock
                    {
                        Text = avatarString,
                        FontSize = 72,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        Foreground = new SolidColorBrush(foregroundColor),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    
                    // Panel oluştur ve içine daire ve yazıyı ekle
                    var panel = new Avalonia.Controls.Panel
                    {
                        Width = size,
                        Height = size
                    };
                    panel.Children.Add(circle);
                    panel.Children.Add(textBlock);
                    
                    // Görüntüyü çiz
                    panel.Measure(new Avalonia.Size(size, size));
                    panel.Arrange(new Avalonia.Rect(0, 0, size, size));
                    panel.Render(drawingContext);
                }
                
                // Stream'e kaydet
                var ms = new MemoryStream();
                renderTarget.Save(ms);
                ms.Position = 0;
                return ms;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Avatarı oluşturma hatası");
                return new MemoryStream(); // Boş bir stream döndür
            }
        }

        //public static async Task SignApp()
        //{
        //    try
        //    {
        //        using (HttpClient client2 = new HttpClient())
        //        {
        //            client2.BaseAddress = new Uri(BASE_URL);
        //            client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //            HttpResponseMessage response = await client2.GetAsync($"api/Firestore/SignApp");
        //            if (response.IsSuccessStatusCode)
        //            {
        //                var result = await response.Content.ReadAsStringAsync();
        //                Log.Information(result);
        //            }
        //            else if (response.StatusCode == HttpStatusCode.Unauthorized)
        //            {
        //                var signInResult = await SignIn(AppSettingsManager.appSettings.FireStoreEmail, AppSettingsManager.appSettings.FireStorePassword);
        //                if (signInResult.Success)
        //                {
        //                    await SignApp();
        //                }
        //                else
        //                {
        //                    Log.Error(signInResult.ErrorMessage);
        //                }
        //            }
        //            else
        //            {
        //                Log.Error(response.StatusCode.ToString());
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Error("Error on Sign App" +e.Message);
        //    }
        //}

        //public static async Task UnSignApp()
        //{
        //    try
        //    {
        //        using (HttpClient client2 = new HttpClient())
        //        {
        //            client2.BaseAddress = new Uri(BASE_URL);
        //            client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //            HttpResponseMessage response = await client2.GetAsync($"api/Firestore/UnSignApp");
        //            if (response.IsSuccessStatusCode)
        //            {
        //                var result = await response.Content.ReadAsStringAsync();
        //                Log.Information(result);
        //            }
        //            else if (response.StatusCode == HttpStatusCode.Unauthorized)
        //            {
        //                var signInResult = await SignIn(AppSettingsManager.appSettings.FireStoreEmail, AppSettingsManager.appSettings.FireStorePassword);
        //                if (signInResult.Success)
        //                {
        //                    await UnSignApp();
        //                }
        //                else
        //                {
        //                    Log.Error(signInResult.ErrorMessage);
        //                }
        //            }
        //            else
        //            {
        //                Log.Error(response.StatusCode.ToString());
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Error("Error on UnSign App" + e.Message);
        //    }
        //}

        public static async Task<bool> IsUserRegistered(string email)
        {
            var collection = database.Collection("Users");

            var querySnapshot = await collection.WhereEqualTo("Email", email).GetSnapshotAsync();

            var userDoc = querySnapshot.Documents.FirstOrDefault();
            if (userDoc != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static MainUser MainUser;
        public static async Task ListenUsers(MainAccountPage mainAccountPage)
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;

                var collection = database.Collection("Users");

                // Kullanıcıyı email ile doğrudan sorgula
                var querySnapshot = await collection.WhereEqualTo("Email", email).GetSnapshotAsync();

                // Eğer kullanıcı bulunduysa, verileri al
                var userDoc = querySnapshot.Documents.FirstOrDefault();
                if (userDoc != null)
                {
                    Dictionary<string, object> user = userDoc.ToDictionary();
                    
                    // MainUser nesnesini UI thread dışında hazırla
                    MainUser = new MainUser
                    {
                        Email = user["Email"].ToString(),
                        ExpiryDate = ((Timestamp)user["ExpiryDate"]).ToDateTime(),
                        ProfileImage = user["UserProfileImage"].ToString(),
                        LastHwidChange = ((Timestamp)user["LastHwidChange"]).ToDateTime()
                    };

                    // Profil resmi ve tarih gibi ağır işlemleri arka planda yap
                    var profileImage = await DownloadProfilePhoto(MainUser.ProfileImage, true);
                    var expiryText = await GetRelativeSubEndTime(MainUser.ExpiryDate);
                    
                    // UI güncellemelerini Dispatcher.UIThread içinde yap
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try 
                        {
                            MainUser.ProfileImageBitmap = profileImage;
                            
                            // UI kontrollerini güvenli bir şekilde güncelle
                            if (mainAccountPage != null)
                            {
                                if (mainAccountPage.Gravatar != null)
                                    mainAccountPage.Gravatar.Source = profileImage;
                                    
                                if (mainAccountPage.TextBlockUsername != null)
                                    mainAccountPage.TextBlockUsername.Text = AppSettingsManager.appSettings.FireStoreDisplayName;
                                    
                                if (mainAccountPage.EmailTextBlock != null)
                                    mainAccountPage.EmailTextBlock.Text = MainUser.Email;
                                    
                                if (mainAccountPage.ExpireTextBlock != null)
                                {
                                    mainAccountPage.ExpireTextBlock.Text = expiryText;
                                    mainAccountPage.ExpireTextBlock.Foreground = ExpiryDateTextColor;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"UI update error in ListenUsers: {ex.Message}");
                        }
                    });
                }
                else
                {
                    await Register(new SubPlan() { PlanName = "lifetime" });
                    await ListenUsers(mainAccountPage);
                }
            }
            catch (Exception e)
            {
               Console.WriteLine($"Error in ListenUsers: {e.Message}\n{e.StackTrace}");
            }
        }


        public static ObservableCollection<WatchHistory> WatchHistories = new ObservableCollection<WatchHistory>();
        public static async Task<GetWatchHistoryResult> GetWatchHistory()
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;

                var collection = database.Collection("WatchHistory").Document(email).Collection("History");

                // WatchHistory verilerini LastWatchDate'e göre sıralayarak çek
                var querySnapshot = await collection.OrderByDescending("LastWatchDate").GetSnapshotAsync();

                // Sonuçları al ve WatchHistory listesine ekle
                var watchHistories = querySnapshot.Documents.Select(snap =>
                {
                    var watchObjects = snap.ToDictionary();
                    return new WatchHistory
                    {
                        Id = Int32.Parse(watchObjects["ShowId"].ToString()),
                        ShowType = (ShowType)Int32.Parse(watchObjects["ShowType"].ToString()),
                        LastWatchDate = ((Timestamp)watchObjects["LastWatchDate"]).ToDateTime(),
                        Name = watchObjects["Name"].ToString(),
                        SeasonNumber = Int32.Parse(watchObjects["SeasonNumber"].ToString()),
                        EpisodeNumber = Int32.Parse(watchObjects["EpisodeNumber"].ToString()),
                        Poster = watchObjects["Poster"].ToString(),
                        Progress = Double.Parse(watchObjects["Progress"].ToString()),
                        DeletedTorrent = (bool)watchObjects["DeletedTorrent"],
                        TorrentHash = watchObjects["TorrentHash"].ToString()
                    };
                }).ToList();

                return new GetWatchHistoryResult
                {
                    ErrorMessage = "",
                    Success = true,
                    WatchHistories = watchHistories
                };
            }
            catch (Exception e)
            {
                return new GetWatchHistoryResult()
                {
                    ErrorMessage = e.Message,
                    Success = false,
                    WatchHistories = new List<WatchHistory>()
                };
            }
        }
        
         
        public static int TotalWatchHistoryPages;
        public static async Task<GetPaginatedWatchHistoryResult> GetPaginatedWatchHistory(int page)
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;
                int currentPage = page; // Aktif sayfa numarasını alıyoruz
                int pageSize = 20; // Sayfa başına verileri sınırla, varsayılan 10

                var collection = database.Collection("WatchHistory").Document(email).Collection("History");

                // 1. Adım: Verilerin toplam sayısını alıyoruz
                var querySnapshot = await collection.GetSnapshotAsync();
                int totalRecords = querySnapshot.Documents.Count;

                // 2. Adım: Toplam sayfa sayısını hesaplıyoruz
                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                // 3. Adım: Geçerli sayfa numarasının geçerli olup olmadığını kontrol ediyoruz
                if (currentPage < 1 || currentPage > totalPages)
                {
                    return new GetPaginatedWatchHistoryResult()
                    {
                        ErrorMessage = "Geçersiz sayfa numarası",
                        Success = false,
                        WatchHistories = new List<WatchHistory>()
                    };
                }

                // 4. Adım: Sayfa numarasına göre offset hesapla
                int offset = (currentPage - 1) * pageSize;

                // Sayfa verisini al
                var documents = await collection.OrderByDescending("LastWatchDate").Offset(offset).Limit(pageSize).GetSnapshotAsync();

                var watchHistories = documents.Documents.Select(snap =>
                {
                    var watchObjects = snap.ToDictionary();
                    return new WatchHistory
                    {
                        Id = Int32.Parse(watchObjects["ShowId"].ToString()),
                        ShowType = (ShowType)Int32.Parse(watchObjects["ShowType"].ToString()),
                        LastWatchDate = ((Timestamp)watchObjects["LastWatchDate"]).ToDateTime(),
                        Name = watchObjects["Name"].ToString(),
                        SeasonNumber = Int32.Parse(watchObjects["SeasonNumber"].ToString()),
                        EpisodeNumber = Int32.Parse(watchObjects["EpisodeNumber"].ToString()),
                        Poster = watchObjects["Poster"].ToString(),
                        Progress = Double.Parse(watchObjects["Progress"].ToString()),
                        DeletedTorrent = (bool)watchObjects["DeletedTorrent"],
                        TorrentHash = watchObjects["TorrentHash"].ToString()
                    };
                }).ToList();

                foreach (var aWatchHistory in watchHistories)
                {
                    WatchHistories.Add(aWatchHistory);
                }
                TotalWatchHistoryPages = totalPages;
                return new GetPaginatedWatchHistoryResult()
                {
                    ErrorMessage = "",
                    Success = true,
                    WatchHistories = watchHistories,
                    TotalPages = totalPages // Toplam sayfa sayısını da ekliyoruz
                };
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return new GetPaginatedWatchHistoryResult()
                {
                    ErrorMessage = e.Message,
                    Success = false,
                    WatchHistories = new List<WatchHistory>()
                };
            }
        }

        public static async Task<FindWatchHistoryResult> FindWatchHistory(int showId, ShowType showType, int seasonNumber, int episodeNumber)
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;

                var collection = database.Collection("WatchHistory").Document(email).Collection("History");

                // Parametrelere göre sorgu yapıyoruz
                var query = collection
                    .WhereEqualTo("ShowId", showId)
                    .WhereEqualTo("ShowType", (int)showType)  // Enum'u int olarak karşılaştırıyoruz
                    .WhereEqualTo("SeasonNumber", seasonNumber)
                    .WhereEqualTo("EpisodeNumber", episodeNumber);

                // Sorguyu çalıştır ve verileri al
                var querySnapshot = await query.GetSnapshotAsync();

                // Verileri WatchHistory nesnesine dönüştür
                var watchHistories = querySnapshot.Documents.Select(snap =>
                {
                    var watchObjects = snap.ToDictionary();
                    return new WatchHistory
                    {
                        Id = Int32.Parse(watchObjects["ShowId"].ToString()),
                        ShowType = (ShowType)Int32.Parse(watchObjects["ShowType"].ToString()),
                        LastWatchDate = ((Timestamp)watchObjects["LastWatchDate"]).ToDateTime(),
                        Name = watchObjects["Name"].ToString(),
                        SeasonNumber = Int32.Parse(watchObjects["SeasonNumber"].ToString()),
                        EpisodeNumber = Int32.Parse(watchObjects["EpisodeNumber"].ToString()),
                        Poster = watchObjects["Poster"].ToString(),
                        Progress = Double.Parse(watchObjects["Progress"].ToString()),
                        DeletedTorrent = (bool)watchObjects["DeletedTorrent"],
                        TorrentHash = watchObjects["TorrentHash"].ToString()
                    };
                }).ToList();

                return new FindWatchHistoryResult()
                {
                    ErrorMessage = "",
                    Success = true,
                    WatchHistory = watchHistories.Count > 0 ? watchHistories.FirstOrDefault() : null
                };
            }
            catch (Exception e)
            {
                return new FindWatchHistoryResult()
                {
                    ErrorMessage = e.Message,
                    Success = false,
                    WatchHistory = null
                };
            }
        }

        public static async Task<AddShowToWatchHistoryResult> AddShowToWatchHistory(AddShowToWatchHistoryRequest addShowToWatchHistoryRequest)
        {
            try
            {
                string email = addShowToWatchHistoryRequest.Email;
                int showId = addShowToWatchHistoryRequest.Id;
                ShowType showType = addShowToWatchHistoryRequest.ShowType;
                DateTime lastWatchDate = addShowToWatchHistoryRequest.LastWatchDate;
                int seasonNumber = addShowToWatchHistoryRequest.SeasonNumber;
                int episodeNumber = addShowToWatchHistoryRequest.EpisodeNumber;
                string name = addShowToWatchHistoryRequest.Name;
                string poster = addShowToWatchHistoryRequest.Poster;
                double progress = addShowToWatchHistoryRequest.Progress;
                string hash = addShowToWatchHistoryRequest.TorrentHash;

                var collection = database.Collection("WatchHistory")
                    .Document(email)
                    .Collection("History");

                var query = collection
                    .WhereEqualTo("ShowId", showId)
                    .WhereEqualTo("ShowType", showType)
                    .WhereEqualTo("SeasonNumber", seasonNumber)
                    .WhereEqualTo("EpisodeNumber", episodeNumber);

                var querySnapshot = await query.GetSnapshotAsync();

                if (querySnapshot.Count > 0)
                {
                    return new AddShowToWatchHistoryResult()
                    {
                        Success = false,
                        ErrorMessage = "This show is already exist in Watch history"
                    };
                }

                var doc = collection.Document();

                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"ShowId",showId},
                    {"ShowType",showType},
                    {"LastWatchDate",lastWatchDate},
                    {"Name",addShowToWatchHistoryRequest.Name +
                            (addShowToWatchHistoryRequest.ShowType == ShowType.TvShow
                                ? " S" + addShowToWatchHistoryRequest.SeasonNumber + " E" +
                                  addShowToWatchHistoryRequest.EpisodeNumber
                                : "")},
                    {"SeasonNumber",seasonNumber},
                    {"EpisodeNumber",episodeNumber},
                    {"Poster",poster},
                    {"Progress",progress},
                    {"DeletedTorrent",addShowToWatchHistoryRequest.DeletedTorrent},
                    {"TorrentHash",hash}
                };
                await doc.SetAsync(data);

                WatchHistory watchHistory = new WatchHistory()
                {
                    Id = addShowToWatchHistoryRequest.Id,
                    EpisodeNumber = addShowToWatchHistoryRequest.EpisodeNumber,
                    LastWatchDate = addShowToWatchHistoryRequest.LastWatchDate,
                    Name = addShowToWatchHistoryRequest.Name +
                           (addShowToWatchHistoryRequest.ShowType == ShowType.TvShow
                               ? " S" + addShowToWatchHistoryRequest.SeasonNumber + " E" +
                                 addShowToWatchHistoryRequest.EpisodeNumber
                               : ""),
                    Poster = addShowToWatchHistoryRequest.Poster,
                    Progress = addShowToWatchHistoryRequest.Progress,
                    SeasonNumber = addShowToWatchHistoryRequest.SeasonNumber,
                    ShowType = addShowToWatchHistoryRequest.ShowType,
                    DeletedTorrent = addShowToWatchHistoryRequest.DeletedTorrent,
                    TorrentHash = addShowToWatchHistoryRequest.TorrentHash
                };
                WatchHistories.Insert(0, watchHistory);

                return new AddShowToWatchHistoryResult()
                { Success = true, ErrorMessage = "" };
            }
            catch (Exception e)
            {
                return new AddShowToWatchHistoryResult()
                { ErrorMessage = e.Message, Success = false };
            }
        }
        
        public static async Task AddShowToDownloadHistory(Torrent torrent)
        {
            try
            {
                Console.WriteLine("Adding torrent to history ....");
                string email = AppSettingsManager.appSettings.FireStoreEmail;
        
                var collection = database.Collection("DownloadHistory")
                    .Document(email)
                    .Collection("History");
        
                var query = collection
                    .WhereEqualTo("Hash", torrent.Hash);
        
                var querySnapshot = await query.GetSnapshotAsync();
        
                if (querySnapshot.Count > 0)
                {
                    Console.WriteLine("Already exist in download history");
                    return;
                }
                else
                {
                    Console.WriteLine("Not in history");
                }
        
                var doc = collection.Document();
        
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"Name",torrent.Name} ,
                    {"Size",torrent.Size},
                    {"ImageUrl",torrent.ImageUrl},
                    {"PublishDate",torrent.PublishDate},
                    {"MovieId",torrent.MovieId},
                    {"MovieName",torrent.MovieName},
                    {"IsCompleted",torrent.IsCompleted},
                    {"ShowType",torrent.ShowType},
                    {"SeasonNumber",torrent.SeasonNumber},
                    {"EpisodeNumber",torrent.EpisodeNumber},
                    {"ImdbId",torrent.ImdbId},
                    {"Hash",torrent.Hash},
                    {"AddedDate",GetNistTime().Result.ToUniversalTime()}
                };
                await doc.SetAsync(data);
        
                Console.WriteLine("Set: " + torrent.Hash);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
        }
        
        public static async Task<List<Torrent>> GetDownloadHistory()
        {
            try
            {
                string email = AppSettingsManager.appSettings.FireStoreEmail;

                var collection = database.Collection("DownloadHistory").Document(email).Collection("History");

                var querySnapshot = await collection.OrderByDescending("AddedDate").GetSnapshotAsync();

                // Sonuçları al ve WatchHistory listesine ekle
                var downloadHistories = querySnapshot.Documents.Select(snap =>
                {
                    var watchObjects = snap.ToDictionary();
                    return new Torrent()
                    {
                        ShowType = Int32.Parse(watchObjects["ShowType"].ToString()),
                        Name = watchObjects["Name"].ToString(),
                        Size = double.Parse(watchObjects["Size"].ToString()),
                        SeasonNumber = Int32.Parse(watchObjects["SeasonNumber"].ToString()),
                        EpisodeNumber = Int32.Parse(watchObjects["EpisodeNumber"].ToString()),
                        ImageUrl =  watchObjects["ImageUrl"].ToString(),
                        PublishDate = watchObjects["PublishDate"].ToString(),
                        MovieId = Int32.Parse(watchObjects["MovieId"].ToString()),
                        MovieName = watchObjects["MovieName"].ToString(),
                        IsCompleted = Boolean.Parse(watchObjects["MovieName"].ToString()),
                        ImdbId =  Int32.Parse(watchObjects["ImdbId"].ToString()),
                        Hash = watchObjects["Hash"].ToString()
                    };
                }).ToList();

                return downloadHistories;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
                return null;
              
            }
        }
        
          public static async Task EditDownloadHistory(string hash,bool IsCompleted)
        {
            try
            {
                var collection = database.Collection("DownloadHistory").Document(AppSettingsManager.appSettings.FireStoreEmail).Collection("History");
                
                var query = collection
                    .WhereEqualTo("Hash", hash);
                
                var querySnapshot = await query.GetSnapshotAsync();

                if (querySnapshot.Count > 0)
                {
                    var doc = querySnapshot.Documents.FirstOrDefault();
                    if (doc != null)
                    {
                        // Gerekli alanları güncelle
                        await doc.Reference.UpdateAsync(new Dictionary<string, object>
                        {
                            { "IsCompleted", IsCompleted},
                        });
                    }

                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
        }
          
          public static async Task DeleteDownloadHistory(string hash)
        {
            try
            {
                var collection = database.Collection("DownloadHistory").Document(AppSettingsManager.appSettings.FireStoreEmail).Collection("History");
                
                var documentRefs = await collection.ListDocumentsAsync().ToListAsync();

                // Find documents where the commentId matches
                var documentsToDelete = documentRefs.Where(docRef =>
                {
                    var snap = docRef.GetSnapshotAsync().Result;
                    var comments = snap.ToDictionary();
                    return comments["Hash"].ToString() == hash;
                }).ToList();

                // Delete matched documents
                var deleteTasks = documentsToDelete.Select(docRef => docRef.DeleteAsync()).ToList();

                await Task.WhenAll(deleteTasks);
                Console.WriteLine("Deleted: " + hash);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        public static async Task<EditWatchHistoryResult> EditWatchHistory(EditWatchHistoryRequest editWatchHistoryRequest)
        {
            try
            {
                var collection = database.Collection("WatchHistory").Document(editWatchHistoryRequest.Email).Collection("History");

                // Veritabanındaki tüm dokümanları sorgulama yerine, doğrudan bir sorgu kullanarak filtreleme yapalım.
                var querySnapshot = await collection
                    .WhereEqualTo("ShowId", editWatchHistoryRequest.Id)
                    .WhereEqualTo("ShowType", (int)editWatchHistoryRequest.ShowType)
                    .WhereEqualTo("SeasonNumber", editWatchHistoryRequest.SeasonNumber)
                    .WhereEqualTo("EpisodeNumber", editWatchHistoryRequest.EpisodeNumber)
                    .GetSnapshotAsync();

                // Filtreleme sonucu bulunan ilk dokümanı al
                var doc = querySnapshot.Documents.FirstOrDefault();
                if (doc != null)
                {
                    // Gerekli alanları güncelle
                    await doc.Reference.UpdateAsync(new Dictionary<string, object>
                    {
                        { "Progress", editWatchHistoryRequest.NewProgress },
                        { "LastWatchDate", Timestamp.FromDateTime(editWatchHistoryRequest.LastWatchDate) },
                        { "DeletedTorrent", editWatchHistoryRequest.DeletedTorrent },
                        { "TorrentHash", editWatchHistoryRequest.Hash }
                    });

                    var currentWatchHistory = WatchHistories.FirstOrDefault(x => x.Id == editWatchHistoryRequest.Id
                        && x.ShowType == editWatchHistoryRequest.ShowType
                        && x.SeasonNumber == editWatchHistoryRequest.SeasonNumber
                        && x.EpisodeNumber == editWatchHistoryRequest.EpisodeNumber);
                    if (currentWatchHistory != null)
                    {
                        currentWatchHistory.Progress = editWatchHistoryRequest.NewProgress;
                        currentWatchHistory.LastWatchDate = editWatchHistoryRequest.LastWatchDate;
                        currentWatchHistory.DeletedTorrent = editWatchHistoryRequest.DeletedTorrent;

                        WatchHistories.Remove(currentWatchHistory);
                        WatchHistories.Insert(0, currentWatchHistory);
                    }
                    else
                    {
                        var watchHistory = await FindWatchHistory(editWatchHistoryRequest.Id, editWatchHistoryRequest.ShowType,
                            editWatchHistoryRequest.SeasonNumber, editWatchHistoryRequest.EpisodeNumber);

                        if (watchHistory != null)
                            WatchHistories.Insert(0, watchHistory.WatchHistory);
                    }

                    return new EditWatchHistoryResult()
                        { ErrorMessage = "", Success = true };
                }

                return new EditWatchHistoryResult()
                    { ErrorMessage = "Couldn't find watch history", Success = false };
            }
            catch (Exception e)
            {
                return new EditWatchHistoryResult()
                    { ErrorMessage = e.Message, Success = false };
            }
        }

        public static async Task<DeleteTorrentWatchHistoryResult> DeleteTorrentWatchHistory(DeleteTorrentWatchHistoryRequest deleteTorrentWatchHistoryRequest)
        {
            try
            {

                var collection = database.Collection("WatchHistory").Document(deleteTorrentWatchHistoryRequest.Email).Collection("History");

                // Veritabanındaki tüm dokümanları almak yerine doğrudan bir sorgu yaparak filtreleme yapalım.
                var querySnapshot = await collection
                    .WhereEqualTo("ShowId", deleteTorrentWatchHistoryRequest.ShowId)
                    .WhereEqualTo("ShowType", (int)deleteTorrentWatchHistoryRequest.ShowType)
                    .GetSnapshotAsync();

                var doc = querySnapshot.Documents.FirstOrDefault();
                if (doc != null)
                {
                    // "DeletedTorrent" alanını güncelle
                    await doc.Reference.UpdateAsync("DeletedTorrent", deleteTorrentWatchHistoryRequest.DeletedTorrent);

                    foreach (var x in WatchHistories)
                    {
                        if (x.Id == deleteTorrentWatchHistoryRequest.ShowId
                            && x.ShowType == deleteTorrentWatchHistoryRequest.ShowType)
                        {
                            x.DeletedTorrent = deleteTorrentWatchHistoryRequest.DeletedTorrent;
                        }
                    }

                    return new DeleteTorrentWatchHistoryResult()
                        { ErrorMessage = "", Success = true };
                }

                return new DeleteTorrentWatchHistoryResult()
                    { ErrorMessage = "Couldn't find watch history", Success = false };
            }
            catch (Exception e)
            {
                return new DeleteTorrentWatchHistoryResult()
                    { ErrorMessage = e.Message, Success = false };
            }
        }


        //public static DateTime GetNistTime()
        //{
        //    var myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.microsoft.com");
        //    var response = myHttpWebRequest.GetResponse();
        //    string todaysDates = response.Headers["date"];
        //    return DateTime.ParseExact(todaysDates,
        //        "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
        //        CultureInfo.InvariantCulture.DateTimeFormat,
        //        DateTimeStyles.AssumeUniversal);
        //}
        private static readonly HttpClient client = new HttpClient();

        public static async Task<DateTime> GetNistTime()
        {
            try
            {
                // HTTP isteği gönder
                var response = await client.GetAsync("https://www.microsoft.com");
                response.EnsureSuccessStatusCode();

                // Tarih başlığını al
                if (response.Headers.TryGetValues("Date", out var values))
                {
                    var dateHeader = values.First();
                    // Tarih başlığını DateTime türüne dönüştür
                    if (DateTime.TryParseExact(dateHeader,
                            "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                            CultureInfo.InvariantCulture.DateTimeFormat,
                            DateTimeStyles.AssumeUniversal,
                            out DateTime networkDateTime))
                    {
                        return networkDateTime.ToLocalTime();
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda sistem saatini döndür
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }

            return DateTime.Now;
        }


        //public static DateTime GetNistTime()
        //{
        //    try
        //    {
        //        const string ntpServer = "pool.ntp.org";
        //        byte[] ntpData = new byte[48];
        //        ntpData[0] = 0x1B; // NTP paketinin başlatılması

        //        // NTP sunucusuna bağlan
        //        var addresses = Dns.GetHostAddresses(ntpServer);
        //        var endPoint = new IPEndPoint(addresses[0], 123);

        //        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        //        {
        //            socket.Connect(endPoint);
        //            socket.Send(ntpData);
        //            socket.ReceiveTimeout = 5000;
        //            socket.Receive(ntpData);
        //            socket.Close();
        //        }

        //        const byte serverReplyTime = 40;
        //        ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
        //        ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

        //        intPart = SwapEndianness(intPart);
        //        fractPart = SwapEndianness(fractPart);

        //        var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
        //        var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

        //        return networkDateTime.ToLocalTime();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        Log.Error(e.Message);
        //        return DateTime.MinValue;
        //    }
        //}

        //public static DateTime GetNistTime()
        //{
        //    try
        //    {
        //        using (var client = new HttpClient())
        //        {
        //            client.Timeout = TimeSpan.FromSeconds(5);
        //            HttpResponseMessage response = client.GetAsync("https://google.com").Result;

        //            if (response.IsSuccessStatusCode)
        //            {
        //                IEnumerable<string> values;
        //                if (response.Headers.TryGetValues("Date", out values))
        //                {
        //                    string dateHeader = values.FirstOrDefault();
        //                    if (DateTime.TryParse(dateHeader, out DateTime googleTime))
        //                    {
        //                        return googleTime.ToUniversalTime();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine($"GetGoogleTime error: {e.Message}");
        //    }

        //    return DateTime.MinValue;
        //}


        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) |
                          ((x & 0x0000ff00) << 8) |
                          ((x & 0x00ff0000) >> 8) |
                          ((x & 0xff000000) >> 24));
        }

        /*public static async Task GetMoviePhotos(Movie selectedMovie, MovieDetailsPhotosPage movieDetailsPhotosPage)
        {
            try
            {
                // ... önceki kodlar ...
                
                // WPF stilinde Dispatcher yerine Avalonia stilinde Dispatcher kullanımı
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    movieDetailsPhotosPage.BackDropsImageCounter.Text = MovieImages.Backdrops.Count + " images";
                    movieDetailsPhotosPage.PosterImageCounter.Text = MovieImages.Posters.Count + " images";
                });
                
                // ... sonraki kodlar ...
            }
            catch (Exception e)
            {
                // ... hata yönetimi ...
            }
        }*/
    }
    public class UploadProfilePhotoResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class DeleteTorrentWatchHistoryRequest
    {
        public string Email { get; set; }
        public int ShowId { get; set; }
        public ShowType ShowType { get; set; }
        public bool DeletedTorrent { get; set; }
    }

    public class DeleteTorrentWatchHistoryResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EditWatchHistoryRequest
    {
        public string Email { get; set; }
        public int Id { get; set; }
        public ShowType ShowType { get; set; }
        public DateTime LastWatchDate { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public double NewProgress { get; set; }
        public bool DeletedTorrent { get; set; }
        public string Hash { get; set; }
    }

    public class EditWatchHistoryResult
    {
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }
    }

    public class GetWatchHistoryRequest
    {
        public string Email { get; set; }
    }

    public class GetWatchHistoryResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<WatchHistory> WatchHistories { get; set; }
    }

    public class AddShowToWatchHistoryRequest
    {
        public string Email { get; set; }
        public int Id { get; set; }
        public ShowType ShowType { get; set; }
        public string Name { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string Poster { get; set; }
        public DateTime LastWatchDate { get; set; }
        public double Progress { get; set; }
        public bool DeletedTorrent { get; set; }
        public string TorrentHash { get; set; }
    }

    public class AddShowToWatchHistoryResult
    {
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }
    }

    public class EditCommentRequest
    {
        public int ShowId { get; set; }
        public ShowType ShowType { get; set; }
        public string CommentId { get; set; }
        public string NewText { get; set; }
        public string Email { get; set; }
    }

    public class EditCommentResult
    {
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }
    }

    public class Error
    {
        public int code { get; set; }
        public string message { get; set; }
        public List<Error2> errors { get; set; }
    }

    public class Error2
    {
        public string message { get; set; }
        public string domain { get; set; }
        public string reason { get; set; }
    }

    public class Root
    {
        public Error error { get; set; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }


    public class SendEmailVerificationRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class SendEmailVerificationResult
    {
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }
    }
    public class Comment : INotifyPropertyChanged
    {
        private string email;
        public string Email
        {
            get
            {
                return email;
            }
            set
            {
                email = value;
                OnPropertyChanged("Email");
            }
        }
        private string id;

        public string Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                OnPropertyChanged("Id");
            }
        }

        private string displayName;
        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                displayName = value;
                OnPropertyChanged("DisplayName");
            }
        }


        private string text;
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
                OnPropertyChanged("Text");
            }
        }

        private DateTime date;
        public DateTime Date
        {
            get
            {
                return date;
            }
            set
            {
                date = value;
                OnPropertyChanged("Date");
            }
        }

        private string replyToCommentId;
        public string ReplyToCommentId
        {
            get
            {
                return replyToCommentId;
            }
            set
            {
                replyToCommentId = value;
                OnPropertyChanged("ReplyToCommentId");
            }
        }

        private int likeCount;
        public int LikeCount
        {
            get
            {
                return likeCount;
            }
            set
            {
                likeCount = value;
                OnPropertyChanged("LikeCount");
            }
        }


        private int dislikeCount;
        public int DislikeCount
        {
            get
            {
                return dislikeCount;
            }
            set
            {
                dislikeCount = value;
                OnPropertyChanged("DislikeCount");
            }
        }

        private IImage profileImage;
        public IImage ProfileImage
        {
            get
            {
                return profileImage;
            }
            set
            {
                profileImage = value;
                OnPropertyChanged("ProfileImage");
            }
        }

        private string profilePhoto;
        public string ProfilePhoto
        {
            get
            {
                return profilePhoto;
            }
            set
            {
                profilePhoto = value;
                OnPropertyChanged("ProfilePhoto");
            }
        }

        private string relativeDate;
        public string RelativeDate
        {
            get
            {
                return relativeDate;
            }
            set
            {
                relativeDate = value;
                OnPropertyChanged("RelativeDate");
            }
        }

        private ObservableCollection<Comment> replyComments;
        public ObservableCollection<Comment> ReplyComments
        {
            get
            {
                return replyComments;
            }
            set
            {
                replyComments = value;
                OnPropertyChanged("ReplyComments");
            }
        }

        private string replyCommentCount;
        public string ReplyCommentCount
        {
            get
            {
                return replyCommentCount;
            }
            set
            {
                replyCommentCount = value;
                OnPropertyChanged("ReplyCommentCount");
            }
        }

        private ObservableCollection<string> likedBy;
        public ObservableCollection<string> LikedBy
        {
            get
            {
                return likedBy;
            }
            set
            {
                likedBy = value;
                OnPropertyChanged("LikedBy");
            }
        }

        private ObservableCollection<string> dislikedBy;
        public ObservableCollection<string> DisLikedBy
        {
            get
            {
                return dislikedBy;
            }
            set
            {
                dislikedBy = value;
                OnPropertyChanged("DisLikedBy");
            }
        }

        private bool likedByMe;
        public bool LikedByMe
        {
            get
            {
                if (LikedBy == null)
                {
                    return false;
                }
                else
                {
                    return LikedBy.Any(x => x == AppSettingsManager.appSettings.FireStoreEmail);
                }
            }
            set
            {
                likedByMe = value;
                OnPropertyChanged("LikedByMe");
            }
        }

        private bool dislikedByMe;
        public bool DislikedByMe
        {
            get
            {
                if (DisLikedBy == null)
                {
                    return false;
                }
                else
                {
                    return DisLikedBy.Any(x => x == AppSettingsManager.appSettings.FireStoreEmail);
                }
            }
            set
            {
                dislikedByMe = value;
                OnPropertyChanged("DislikedByMe");
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public enum InteractionType
    {
        Like,
        Dislike
    }

    public enum ErrorType
    {
        Expired,
        Hwid,
        NoError,
        UserNotFound
    }

    public class LoginResult
    {
        public bool Success;
        public ErrorType ErrorType;
        public string ErrorMessage;
        public DateTime ExpiryTime;
        public DateTime CurrentTime;
    }

    public class ChangeProfileImageResult
    {
        public bool Success;
        public string ErrorMessage;
    }

    public enum ChangeHwidErrorType
    {
        Time,
        UserNotFound,
        NoError
    }
    public class ChangeHwidResult
    {
        public ChangeHwidErrorType ChangeHwidErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }
    }

    public class MainUser : INotifyPropertyChanged
    {
        private string email;
        public string Email
        {
            get
            {
                return email;
            }
            set
            {
                email = value;
                OnPropertyChanged("Email");
            }
        }

        private string username;
        public string Username
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
                OnPropertyChanged("Username");
            }
        }

        private DateTime expiryDate;
        public DateTime ExpiryDate
        {
            get
            {
                return expiryDate;
            }
            set
            {
                expiryDate = value;
                OnPropertyChanged("ExpiryDate");
            }
        }

        public string ExpiryDateString
        {
            get
            {
                return FirestoreManager.GetRelativeSubEndTime(ExpiryDate).GetAwaiter().GetResult();
            }
            set
            {
                OnPropertyChanged("ExpiryDateString");
            }
        }

        private DateTime lastHwidChange;
        public DateTime LastHwidChange
        {
            get
            {
                return lastHwidChange;
            }
            set
            {
                lastHwidChange = value;
                OnPropertyChanged("LastHwidChange");
            }
        }

        private string profileImage;
        public string ProfileImage
        {
            get
            {
                return profileImage;
            }
            set
            {
                profileImage = value;
                OnPropertyChanged("ProfileImage");
            }
        }

        private IImage profileImageBitmap;
        public IImage ProfileImageBitmap
        {
            get
            {
                return profileImageBitmap;
            }
            set
            {
                profileImageBitmap = value;
                OnPropertyChanged("ProfileImageBitmap");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class VerificationPayload
    {
        public string requestType { get; set; } = "PASSWORD_RESET";
        public string idToken { get; set; }
    }

    public class RestClient
    {
        public static HttpClient Client { get; private set; }
        public RestClient()
        {
            var platform = PlatformDetector.GetPlatform();
            if (platform == Platform.Windows || platform == Platform.Linux || platform == Platform.Mac)
            {
                ServicePointManager.UseNagleAlgorithm = false;
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SetTcpKeepAlive(false, 0, 0);
                ServicePointManager.DefaultConnectionLimit = 100000;
                
                HttpClientHandler hch = new HttpClientHandler();
                hch.Proxy = null;
                hch.UseProxy = false;
                Client = new HttpClient(hch);
            }
            else
            {
                Client = new HttpClient();
            }
            
            
        }
        public async Task<T> Get<T>(string URL)
        {
            HttpResponseMessage result = await Client.GetAsync(URL);
            Stream stream = await result.Content.ReadAsStreamAsync();
            T responseObject = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream);
            return responseObject;

        }

        public async Task<HttpResponseMessage> Put<T>(string URL, T Content)
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(Content);
            HttpResponseMessage result = await Client.PutAsync(URL,
                new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json"));
            return result;

        }

        public async Task<HttpResponseMessage> Post<T>(string URL, T Content)
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(Content);
            HttpResponseMessage result = await Client.PostAsync(URL,
                new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json"));
            return result;

        }

        public async Task<HttpResponseMessage> Delete(string URL)
        {
            HttpResponseMessage result = await Client.DeleteAsync(URL);
            return result;
        }
    }
    public class RootFirestore
    {
        public string type { get; set; }
        public string project_id { get; set; }
        public string private_key_id { get; set; }
        public string private_key { get; set; }
        public string client_email { get; set; }
        public string client_id { get; set; }
        public string auth_uri { get; set; }
        public string token_uri { get; set; }
        public string auth_provider_x509_cert_url { get; set; }
        public string client_x509_cert_url { get; set; }
        public string universe_domain { get; set; }
    }

    public class UserInfo
    {
        public string ip { get; set; }
        public string hostname { get; set; }
        public string city { get; set; }
        public string region { get; set; }
        public string country { get; set; }
        public string loc { get; set; }
        public string org { get; set; }
        public string postal { get; set; }
        public string timezone { get; set; }
        public string readme { get; set; }
    }
    
  
}
