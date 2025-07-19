using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.Windows.Threading;
using FontAwesome.Sharp;
using HandyControl.Controls;
using MaterialDesignThemes.Wpf;
using Serilog;
using Color = System.Windows.Media.Color;
using TextBox = System.Windows.Controls.TextBox;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for CommentPage.xaml
    /// </summary>
    public partial class CommentPage : Page
    {
        private Movie selectedShow;
        DispatcherTimer timer;
        public CommentPage(Movie selectedShow)
        {
            InitializeComponent();
            this.selectedShow = selectedShow;
            LoadComments();
            this.OnItemsLoaded+= OnOnItemsLoaded;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += (sender, e) => DispatcherTimerForPlayerControlOnTick(sender, e);
            timer.Start();
        }

        private AsyncObservableCollection<Comment> commentLists = new AsyncObservableCollection<Comment>();
        private async void DispatcherTimerForPlayerControlOnTick(object? sender, EventArgs eventArgs)
        {
            try
            {
                foreach ( var comment in commentLists)
                {
                    comment.RelativeDate =await FirestoreManager.GetRelativeDate(comment.Date);
                    if (comment.ReplyComments != null)
                    {
                        foreach (var commentReplyComment in comment.ReplyComments)
                        {
                            commentReplyComment.RelativeDate = await FirestoreManager.GetRelativeDate(commentReplyComment.Date);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        private async void OnOnItemsLoaded(object? sender, OnItemLoadingFinishedEventArgs e)
        {
            try
            {
                commentLists = e.comments;
                var comments = e.comments;
                int replyCommentCounter = 0;
                for (int i = 0; i < comments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    var currentMainComment = CommentsDisplay.Items[i] as Comment;
                    if(currentMainComment == null) continue;
                    if (currentMainComment.ReplyComments != null)
                    {
                        replyCommentCounter += currentMainComment.ReplyComments.Count;
                    }
                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            Expander expander = FindDescendantByName<Expander>(lbi, "ExpanderReplyComments");
                            if (expander != null)
                            {
                                if (currentMainComment.ReplyComments == null || currentMainComment.ReplyComments.Count == 0)
                                {
                                    expander.Visibility = Visibility.Collapsed;
                                }
                                else
                                {
                                    expander.Visibility = Visibility.Visible;
                                }
                            }

                            PopupBox popupBox = FindDescendantByName<PopupBox>(lbi, "PopupBoxMainComment");
                            if (popupBox != null)
                            {
                                if (currentMainComment.Email == AppSettingsManager.appSettings.FireStoreEmail)
                                {
                                    popupBox.Visibility = Visibility.Visible;
                                }
                                else
                                {
                                    popupBox.Visibility = Visibility.Collapsed;
                                }
                            }

                            StackPanel stackPanelTextMain = FindDescendantByName<StackPanel>(lbi, "StackPanelTExtMain");
                            if (stackPanelTextMain != null)
                            {
                                stackPanelTextMain.Width = CommentsDisplay.ActualWidth - 300;
                            }

                            IconBlock iconBlockThumbsUp = FindDescendantByName<IconBlock>(lbi, "ThumbsUpMainComment");
                            if (iconBlockThumbsUp != null)
                            {
                                iconBlockThumbsUp.Foreground = currentMainComment.LikedByMe
                                    ? new SolidColorBrush(Colors.White)
                                    : new SolidColorBrush(Color.FromArgb(255, 145, 145, 152));
                                iconBlockThumbsUp.IconFont =
                                    currentMainComment.LikedByMe ? IconFont.Solid : IconFont.Regular;
                            }

                            IconBlock iconBlockThumbsDown = FindDescendantByName<IconBlock>(lbi, "ThumbsDownMainComment");
                            if (iconBlockThumbsDown != null)
                            {
                                iconBlockThumbsDown.Foreground = currentMainComment.DislikedByMe
                                    ? new SolidColorBrush(Colors.White)
                                    : new SolidColorBrush(Color.FromArgb(255, 145, 145, 152));
                                iconBlockThumbsDown.IconFont = currentMainComment.DislikedByMe ?
                                    IconFont.Solid : IconFont.Regular;
                            }

                            ListBox replyListBox = FindDescendantByName<ListBox>(lbi, "ReplyCommentsDisplay");
                            if (replyListBox != null && currentMainComment.ReplyComments != null)
                            {
                                for (int z = 0; z < currentMainComment.ReplyComments.Count; z++)
                                {
                                    var lbiReply = replyListBox.ItemContainerGenerator.ContainerFromIndex(z) as ListBoxItem;
                                    if (lbiReply == null) continue;
                                    var currentReplyComment = replyListBox.Items[z] as Comment;
                                    if (currentReplyComment == null) continue;
                                    PopupBox popupBoxReplyComment = FindDescendantByName<PopupBox>(lbiReply, "PopupBoxReplyComment");
                                    if (popupBoxReplyComment != null)
                                    {
                                        if (currentReplyComment.Email == AppSettingsManager.appSettings.FireStoreEmail)
                                        {
                                            popupBoxReplyComment.Visibility = Visibility.Visible;
                                        }
                                        else
                                        {
                                            popupBoxReplyComment.Visibility = Visibility.Collapsed;
                                        }
                                    }

                                    StackPanel stackPanelTextReply = FindDescendantByName<StackPanel>(lbiReply, "StackPanelTExtReply");
                                    if (stackPanelTextReply != null)
                                    {
                                        stackPanelTextReply.Width = CommentsDisplay.ActualWidth - 300;
                                    }

                                    IconBlock iconBlockThumbsUpReplyComment = FindDescendantByName<IconBlock>(lbiReply, "ThumbsUpReplyComments");
                                    if (iconBlockThumbsUpReplyComment != null)
                                    {
                                        iconBlockThumbsUpReplyComment.Foreground = currentReplyComment.LikedByMe
                                            ? new SolidColorBrush(Colors.White)
                                            : new SolidColorBrush(Color.FromArgb(255, 145, 145, 152));
                                        iconBlockThumbsUpReplyComment.IconFont = currentReplyComment.LikedByMe
                                            ? IconFont.Solid
                                            : IconFont.Regular;
                                    }

                                    IconBlock iconBlockThumbsDownReplyComment = FindDescendantByName<IconBlock>(lbiReply, "ThumbsDownReplyComments");
                                    if (iconBlockThumbsDownReplyComment != null)
                                    {
                                        iconBlockThumbsDownReplyComment.Foreground = currentReplyComment.DislikedByMe
                                            ? new SolidColorBrush(Colors.White)
                                            : new SolidColorBrush(Color.FromArgb(255, 145, 145, 152));
                                        iconBlockThumbsDownReplyComment.IconFont = currentReplyComment.DislikedByMe
                                            ? IconFont.Solid
                                            : IconFont.Regular;
                                    }
                                }
                            }

                        }));
                }

                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.TextBlockCommentCounter.Text = comments.Count + replyCommentCounter + " " + App.Current.Resources["CommentsStringLowerCase"];
                   
                        
                    }));
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }

        }

        public async void LoadComments()
        {
            try
            {
                ViewboxNoCommentsFound.Visibility = Visibility.Collapsed;
                SearchingPanel.Visibility = Visibility.Visible;
                CommentsDisplay.ItemsSource = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                await FirestoreManager.ListenForCommentChanges(selectedShow.Id, selectedShow.ShowType, this);
                ProfileImage.Source = await 
                    FirestoreManager.DownloadProfilePhoto(AppSettingsManager.appSettings.FireStoreProfilePhotoName, true);
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        //private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        //{
        //    var s = sender as ListBox;

        //    var comment = s.DataContext as Comment;
        //    if (comment != null)
        //    {
        //        MessageBox.Show(comment.Text + " " + s.ActualHeight);
        //    }
        //}

        private void CommentPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //FirestoreManager.lis.StopAsync();
                timer.Stop();
                timer.Tick -= DispatcherTimerForPlayerControlOnTick;

                // Clear any references to UI elements or data
                if (CommentsDisplay != null)
                    CommentsDisplay.ItemsSource = null;

                // Clear any other references that might cause memory leaks
                ProfileImage = null;
            }
            catch (System.Exception exception)
            {
                Log.Error(exception.Message);
            }
        }

        private async void CommentSendBtn_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(CommentTextBox.Text))
                {
                    new CustomMessageBox(App.Current.Resources["CommentEmptyError"].ToString(), MessageType.Error, MessageButtons.Ok).ShowDialog();
                }
                else
                {
                    await FirestoreManager.AddComment(selectedShow.Id,selectedShow.ShowType, CommentTextBox.Text,"-1", this);
                    CommentTextBox.Text = "";
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }



        private async void CommentSendReplyButtonPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (selectedIndex == -1) return;
                var selectedComment = CommentsDisplay.Items[selectedIndex] as Comment;
                var selectedListboxItem = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                if (selectedComment == null || selectedComment == null)
                    return;


                TextBox textBoxReply = FindDescendantByName<TextBox>(selectedListboxItem, "CommentTextBoxReply");
                if(textBoxReply == null) return;
                if (String.IsNullOrWhiteSpace(textBoxReply.Text))
                {
                    new CustomMessageBox(App.Current.Resources["ReplyCommentEmptyError"].ToString(), MessageType.Error, MessageButtons.Ok).ShowDialog();
                }
                else
                {
                    await FirestoreManager.AddComment(selectedShow.Id,selectedShow.ShowType, "@" + selectedComment.DisplayName + " " + textBoxReply.Text , selectedComment.Id,this);
                    textBoxReply.Text = "";
                    foreach (var openedReply in OpenedReplys)
                    {
                        openedReply.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        private int selectedIndex = -1;
        private List<Grid> OpenedReplys = new List<Grid>();
        private async void ReplyButtonPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var comments = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                for (int i = 0; i < comments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
                var selectedComment = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                if (selectedComment == null)
                    return;

                foreach (var openedReply in OpenedReplys)
                {
                    openedReply.Visibility = Visibility.Collapsed;
                }

                foreach (var editArea in GridEditAreas)
                {
                    editArea.Visibility = Visibility.Collapsed;
                }
                foreach (var editTextBlock in editTextBlocks)
                {
                    editTextBlock.Visibility = Visibility.Visible;
                }

                Grid gridReply = FindDescendantByName<Grid>(selectedComment, "GridReplyArea");
                gridReply.Visibility = Visibility.Visible;
                gridReply.Width = CommentsDisplay.ActualWidth - 700;
                OpenedReplys.Add(gridReply);


                Gravatar ProfileImageReply = FindDescendantByName<Gravatar>(selectedComment, "ProfileImageReply");
                ProfileImageReply.Source = await FirestoreManager.DownloadProfilePhoto(AppSettingsManager.appSettings.FireStoreProfilePhotoName, true);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public T FindDescendantByName<T>(DependencyObject obj, string objname) where T : DependencyObject
        {
            try
            {
                string controlneve = "";

                Type tyype = obj.GetType();
                if (tyype.GetProperty("Name") != null)
                {
                    PropertyInfo prop = tyype.GetProperty("Name");
                    controlneve = (string)prop.GetValue((object)obj, null);
                }
                else
                {
                    return null;
                }

                if (obj is T && objname.ToString().ToLower() == controlneve.ToString().ToLower())
                {
                    return obj as T;
                }

                // Check for children
                int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
                if (childrenCount < 1)
                    return null;

                // First check all the children
                for (int i = 0; i <= childrenCount - 1; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child is T && objname.ToString().ToLower() == controlneve.ToString().ToLower())
                    {
                        return child as T;
                    }
                }

                // Then check the childrens children
                for (int i = 0; i <= childrenCount - 1; i++)
                {
                    string checkobjname = objname;
                    DependencyObject child = FindDescendantByName<T>(VisualTreeHelper.GetChild(obj, i), objname);
                    if (child != null && child is T && objname.ToString().ToLower() == checkobjname.ToString().ToLower())
                    {
                        return child as T;
                    }
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            return null;
        }

        private static bool IsMouseOverTarget(Visual target, System.Windows.Point point)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(target);
            return bounds.Contains(point);
        }

        private void CommentPage_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                foreach (var openedReply in OpenedReplys)
                {
                    openedReply.Visibility = Visibility.Collapsed;
                }

                foreach (var editArea in GridEditAreas)
                {
                    editArea.Visibility = Visibility.Collapsed;
                }
                foreach (var editTextBlock in editTextBlocks)
                {
                    editTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void CommentSendReplyButton2PreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (selectedIndex == -1) return;
                var selectedMainComment = CommentsDisplay.Items[selectedIndex] as Comment;
                var selectedListboxItem = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                if (selectedMainComment == null || selectedListboxItem == null)
                    return;

                ListBox replyListBox = FindDescendantByName<ListBox>(selectedListboxItem, "ReplyCommentsDisplay");
            
                var selectedREplyComment = replyListBox.ItemContainerGenerator.ContainerFromIndex(replyListboxSelectedIndex) as ListBoxItem;
                if (selectedREplyComment == null) return;

                var selectedReplyCOmment = replyListBox.Items[replyListboxSelectedIndex] as Comment;
                if(selectedReplyCOmment == null) return;

                TextBox textBoxReply2 = FindDescendantByName<TextBox>(selectedREplyComment, "CommentTextBoxReply2");
                if (textBoxReply2 == null) return;
                if (String.IsNullOrWhiteSpace(textBoxReply2.Text))
                {
                    new CustomMessageBox(App.Current.Resources["ReplyCommentEmptyError"].ToString(), MessageType.Error, MessageButtons.Ok).ShowDialog();
                }
                else
                {
                    await FirestoreManager.AddComment(selectedShow.Id,selectedShow.ShowType, "@" + selectedReplyCOmment.DisplayName + " " + textBoxReply2.Text, selectedMainComment.Id, this);
                    textBoxReply2.Text = "";
                    foreach (var openedReply in OpenedReplys)
                    {
                        openedReply.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private int replyListboxSelectedIndex;
        private async void ReplyButton2PreviewMouseleftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var comments = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                for (int i = 0; i < comments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                var selectedComment = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                if (selectedComment == null)
                    return;

                foreach (var openedReply in OpenedReplys)
                {
                    openedReply.Visibility = Visibility.Collapsed;
                }
                foreach (var editArea in GridEditAreas)
                {
                    editArea.Visibility = Visibility.Collapsed;
                }
                foreach (var editTextBlock in editTextBlocks)
                {
                    editTextBlock.Visibility = Visibility.Visible;
                }

                replyListboxSelectedIndex = -1;
                var currentMainComment = CommentsDisplay.Items[selectedIndex] as Comment;
                if (currentMainComment != null)
                {
                    ListBox replyListBox = FindDescendantByName<ListBox>(selectedComment, "ReplyCommentsDisplay");
                    for (int i = 0; i < currentMainComment.ReplyComments.Count; i++)
                    {
                        var lbi = replyListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                        if (lbi == null) continue;
                        if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                        {
                            replyListboxSelectedIndex = i;
                            break;
                        }
                    }
                    var selectedREplyComment = replyListBox.ItemContainerGenerator.ContainerFromIndex(replyListboxSelectedIndex) as ListBoxItem;
                    if (selectedREplyComment == null)
                        return;

                    foreach (var openedReply in OpenedReplys)
                    {
                        openedReply.Visibility = Visibility.Collapsed;
                    }

                    Grid gridReply = FindDescendantByName<Grid>(selectedREplyComment, "GridReply2Area");
                    gridReply.Visibility = Visibility.Visible;
                    gridReply.Width = CommentsDisplay.ActualWidth - 700;
                    OpenedReplys.Add(gridReply);

                    Gravatar ProfileImageReply2 = FindDescendantByName<Gravatar>(selectedREplyComment, "ProfileImageReply2");
                
                    ProfileImageReply2.Source = await FirestoreManager.DownloadProfilePhoto(AppSettingsManager.appSettings.FireStoreProfilePhotoName,true);
                
                
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ThumbsUpMainCOmmentPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var comments = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                for (int i = 0; i < comments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
                var selectedComment = this.CommentsDisplay.Items[selectedIndex] as Comment;
                if (selectedComment == null) return;

                await FirestoreManager.LikeDislikeComment(selectedShow.Id,selectedShow.ShowType,InteractionType.Like,selectedComment.Id, this);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ThumbsDownMainCOmmentPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var comments = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                for (int i = 0; i < comments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
                var selectedComment = this.CommentsDisplay.Items[selectedIndex] as Comment;
                if (selectedComment == null) return;

                await FirestoreManager.LikeDislikeComment(selectedShow.Id,selectedShow.ShowType,InteractionType.Dislike,selectedComment.Id,this);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ThumsUpReplyCommentsPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var mainComments = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                for (int i = 0; i < mainComments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                var selectedMainCommentListBoxItem = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                if (selectedMainCommentListBoxItem == null) return;

                replyListboxSelectedIndex = -1;
                var currentMainComment = CommentsDisplay.Items[selectedIndex] as Comment;
                if (currentMainComment != null)
                {
                    ListBox replyListBox = FindDescendantByName<ListBox>(selectedMainCommentListBoxItem, "ReplyCommentsDisplay");
                    for (int i = 0; i < currentMainComment.ReplyComments.Count; i++)
                    {
                        var lbi = replyListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                        if (lbi == null) continue;
                        if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                        {
                            replyListboxSelectedIndex = i;
                            break;
                        }
                    }

                    var selectedREplyComment =
                        replyListBox.Items[replyListboxSelectedIndex] as Comment;
                    if (selectedREplyComment == null)
                        return;

                    await FirestoreManager.LikeDislikeComment(selectedShow.Id,selectedShow.ShowType,InteractionType.Like, selectedREplyComment.Id, this);
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ThumsDownReplyCommentsPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var mainComments = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                for (int i = 0; i < mainComments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                var selectedMainCommentListBoxItem = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                if (selectedMainCommentListBoxItem == null) return;

                replyListboxSelectedIndex = -1;
                var currentMainComment = CommentsDisplay.Items[selectedIndex] as Comment;
                if (currentMainComment != null)
                {
                    ListBox replyListBox = FindDescendantByName<ListBox>(selectedMainCommentListBoxItem, "ReplyCommentsDisplay");
                    for (int i = 0; i < currentMainComment.ReplyComments.Count; i++)
                    {
                        var lbi = replyListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                        if (lbi == null) continue;
                        if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                        {
                            replyListboxSelectedIndex = i;
                            break;
                        }
                    }

                    var selectedREplyComment =
                        replyListBox.Items[replyListboxSelectedIndex] as Comment;
                    if (selectedREplyComment == null)
                        return;
          
                    await FirestoreManager.LikeDislikeComment(selectedShow.Id,selectedShow.ShowType,InteractionType.Dislike, selectedREplyComment.Id, this);
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public event EventHandler<OnItemLoadingFinishedEventArgs> OnItemsLoaded;

        public virtual void OnItemLoadingFinished(OnItemLoadingFinishedEventArgs e)
        {
            OnItemsLoaded?.Invoke(this, e);
        }
        private List<Grid> GridEditAreas = new List<Grid>();
        private List<TextBlock> editTextBlocks = new List<TextBlock>();
        private string textBeforeEdit = "";
        private void PopupBOxMainCommentEditBtnPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var TextBlock = sender as TextBlock;
                if(TextBlock == null) return;
                var comment = TextBlock.DataContext as Comment;
                if (comment == null) return;

                var mainComments = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                for (int i = 0; i < mainComments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                var selectedMainCommentListBoxItem = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                if (selectedMainCommentListBoxItem == null) return;

                foreach (var editArea in GridEditAreas)
                {
                    editArea.Visibility = Visibility.Collapsed;
                }
                foreach (var editTextBlock in editTextBlocks)
                {
                    editTextBlock.Visibility = Visibility.Visible;
                }

                foreach (var openedReply in OpenedReplys)
                {
                    openedReply.Visibility = Visibility.Collapsed;
                }

                Grid gridEditArea = FindDescendantByName<Grid>(selectedMainCommentListBoxItem, "GridEditArea");
                if (gridEditArea == null) return;
                gridEditArea.Width = CommentsDisplay.ActualWidth - 700;
                gridEditArea.Visibility = Visibility.Visible;
                GridEditAreas.Add(gridEditArea);
                TextBox textBoxReplyEdit = FindDescendantByName<TextBox>(selectedMainCommentListBoxItem, "MainCommentTextBoxEditReply");
                TextBlock TextBlockMainCommentText = FindDescendantByName<TextBlock>(selectedMainCommentListBoxItem, "TextBlockMainCommentText");
                EditSaveButton = FindDescendantByName<Button>(selectedMainCommentListBoxItem, "MainCommentEditReplySendBtn");
                if (textBoxReplyEdit == null || TextBlockMainCommentText == null) return;
                TextBlockMainCommentText.Visibility = Visibility.Collapsed;
                editTextBlocks.Add(TextBlockMainCommentText);
                textBoxReplyEdit.Text = comment.Text;
                EditSaveButton.IsEnabled = false;
                textBeforeEdit = comment.Text;
                textBoxReplyEdit.TextChanged+= TextBoxReplyEditOnTextChanged;
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxReplyEditOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textbox = sender as TextBox;
                if (textbox != null && EditSaveButton != null)
                {
                    if (textBeforeEdit == textbox.Text)
                    {
                        EditSaveButton.IsEnabled = false;
                    }
                    else
                    {
                        EditSaveButton.IsEnabled = true;
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void PopupBOxMainCommentDeleteBtnPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var TextBlock = sender as TextBlock;
                if (TextBlock == null) return;
                var comment = TextBlock.DataContext as Comment;
                if (comment == null) return;
                await FirestoreManager.DeleteComment(selectedShow.Id,selectedShow.ShowType, comment.Id, this);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PopupBOxReplyCommentEditBtnPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var mainComments = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                for (int i = 0; i < mainComments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                var selectedMainCommentListBoxItem = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                var currentMainComment = CommentsDisplay.Items[selectedIndex] as Comment;
                if (selectedMainCommentListBoxItem == null || currentMainComment == null) return;

                ListBox replyListBox = FindDescendantByName<ListBox>(selectedMainCommentListBoxItem, "ReplyCommentsDisplay");
                var current = sender as TextBlock;
                if (current == null) return;
                var currentReply = current.DataContext as Comment;
                for (int z = 0; z < currentMainComment.ReplyComments.Count; z++)
                {
                    var lbiReply = replyListBox.ItemContainerGenerator.ContainerFromIndex(z) as ListBoxItem;
                    var reply = replyListBox.Items[z] as Comment;
                    if (lbiReply == null) continue;
                    if (IsMouseOverTarget(lbiReply, e.GetPosition((IInputElement)lbiReply)) && currentReply.Id == reply.Id)
                    {
                        replyListboxSelectedIndex = z;
                        break;
                    }
                }

                foreach (var editArea in GridEditAreas)
                {
                    editArea.Visibility = Visibility.Collapsed;
                }
                foreach (var editTextBlock in editTextBlocks)
                {
                    editTextBlock.Visibility = Visibility.Visible;
                }
                foreach (var openedReply in OpenedReplys)
                {
                    openedReply.Visibility = Visibility.Collapsed;
                }
                var selectedReplyCommentListboxItem = replyListBox.ItemContainerGenerator.ContainerFromIndex(replyListboxSelectedIndex) as ListBoxItem;
                var selectedReplyCommentTExt = replyListBox.Items[replyListboxSelectedIndex] as Comment;
                if (selectedReplyCommentListboxItem == null) return;

                Grid gridEditArea = FindDescendantByName<Grid>(selectedReplyCommentListboxItem, "GridEditReplyArea");
                if (gridEditArea == null) return;
                gridEditArea.Width = CommentsDisplay.ActualWidth - 700;
                gridEditArea.Visibility = Visibility.Visible;
                GridEditAreas.Add(gridEditArea);
                TextBox textBoxReplyEdit = FindDescendantByName<TextBox>(selectedReplyCommentListboxItem, "ReplyCommentTextBoxEditReply");
                TextBlock TextBlockMainCommentText = FindDescendantByName<TextBlock>(selectedReplyCommentListboxItem, "TextBlockTextReplyComment");
                EditSaveButton = FindDescendantByName<Button>(selectedReplyCommentListboxItem, "ReplyCommentEditReplySendBtn");
                if (textBoxReplyEdit == null || TextBlockMainCommentText == null) return;
                TextBlockMainCommentText.Visibility = Visibility.Collapsed;
                editTextBlocks.Add(TextBlockMainCommentText);
                textBoxReplyEdit.Text = selectedReplyCommentTExt.Text;
                EditSaveButton.IsEnabled = false;
                textBeforeEdit = selectedReplyCommentTExt.Text;
                textBoxReplyEdit.TextChanged += TextBoxReplyEditOnTextChanged;
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void PopupBOxReplyCommentDeleteBtnPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var TextBlock = sender as TextBlock;
                if (TextBlock == null) return;
                var comment = TextBlock.DataContext as Comment;
                if (comment == null) return;
                await FirestoreManager.DeleteComment(selectedShow.Id,selectedShow.ShowType, comment.Id,this);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private Button EditSaveButton;
        private async void MainCommentEditReplySendBtnPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var selectedMainCommentListBoxItem = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                var selectedMainComment = CommentsDisplay.Items[selectedIndex] as Comment;
                if (selectedMainCommentListBoxItem == null || selectedMainComment == null) return;

                Grid gridEditArea = FindDescendantByName<Grid>(selectedMainCommentListBoxItem, "GridEditArea");
                if (gridEditArea == null) return;
                gridEditArea.Visibility = Visibility.Collapsed;
                TextBox textBoxReplyEdit = FindDescendantByName<TextBox>(selectedMainCommentListBoxItem, "MainCommentTextBoxEditReply");
                TextBlock TextBlockMainCommentText = FindDescendantByName<TextBlock>(selectedMainCommentListBoxItem, "TextBlockMainCommentText");
                if (textBoxReplyEdit == null || TextBlockMainCommentText == null) return;
                TextBlockMainCommentText.Visibility = Visibility.Visible;
                await FirestoreManager.EditComment(selectedShow.Id, selectedShow.ShowType, selectedMainComment.Id,
                    textBoxReplyEdit.Text);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MainCommentCancelEditReplySendBtnPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                foreach (var editArea in GridEditAreas)
                {
                    editArea.Visibility = Visibility.Collapsed;
                }
                foreach (var editTextBlock in editTextBlocks)
                {
                    editTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ReplyCommentsDisplayLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs(selectedShow.ShowType == ShowType.Movie ? FirestoreManager.MovieComments : FirestoreManager.TvShowComments));
                var replyListBox = sender as ListBox;
           
                var currentMainComment = replyListBox.DataContext as Comment;
                if (currentMainComment != null && replyListBox != null && currentMainComment.ReplyComments != null)
                {
                    for (int z = 0; z < currentMainComment.ReplyComments.Count; z++)
                    {
                        var lbiReply = replyListBox.ItemContainerGenerator.ContainerFromIndex(z) as ListBoxItem;
                        if (lbiReply == null) continue;
                        var currentReplyComment = replyListBox.Items[z] as Comment;
                        if (currentReplyComment == null) continue;
                        StackPanel stackPanelTextReply = FindDescendantByName<StackPanel>(lbiReply, "StackPanelTExtReply");
                        if (stackPanelTextReply != null)
                        {
                            stackPanelTextReply.Width = CommentsDisplay.ActualWidth - 300;
                        }
                    }
                    for (int z = 0; z < currentMainComment.ReplyComments.Count; z++)
                    {
                        var lbiReply = replyListBox.ItemContainerGenerator.ContainerFromIndex(z) as ListBoxItem;
                        if (lbiReply == null) continue;
                        var currentReplyComment = replyListBox.Items[z] as Comment;
                        if (currentReplyComment == null) continue;
                        PopupBox popupBoxReplyComment = FindDescendantByName<PopupBox>(lbiReply, "PopupBoxReplyComment");
                        if (popupBoxReplyComment != null)
                        {
                            if (currentReplyComment.Email == AppSettingsManager.appSettings.FireStoreEmail)
                            {
                                popupBoxReplyComment.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                popupBoxReplyComment.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ReplyCommentEditReplySendBtnPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var selectedMainCommentListBoxItem = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
                var currentMainComment = CommentsDisplay.Items[selectedIndex] as Comment;
                if (selectedMainCommentListBoxItem == null || currentMainComment == null) return;

                ListBox replyListBox = FindDescendantByName<ListBox>(selectedMainCommentListBoxItem, "ReplyCommentsDisplay");
                var selectedReplyCommentListBoxItem = replyListBox.ItemContainerGenerator.ContainerFromIndex(replyListboxSelectedIndex) as ListBoxItem;
                var selectedReplyComment = replyListBox.Items[replyListboxSelectedIndex] as Comment;
                if (selectedReplyCommentListBoxItem == null || selectedReplyComment == null) return;


                Grid gridEditArea = FindDescendantByName<Grid>(selectedReplyCommentListBoxItem, "GridEditReplyArea");
                if (gridEditArea == null) return;
                gridEditArea.Visibility = Visibility.Collapsed;
                TextBox textBoxReplyEdit = FindDescendantByName<TextBox>(selectedReplyCommentListBoxItem, "ReplyCommentTextBoxEditReply");
                TextBlock TextBlockMainCommentText = FindDescendantByName<TextBlock>(selectedReplyCommentListBoxItem, "TextBlockTextReplyComment");
                if (textBoxReplyEdit == null || TextBlockMainCommentText == null) return;
                TextBlockMainCommentText.Visibility = Visibility.Visible;
                await FirestoreManager.EditComment(selectedShow.Id, selectedShow.ShowType, selectedReplyComment.Id,
                    textBoxReplyEdit.Text);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ReplyCommentCancelEditReplySendBtnPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                foreach (var editArea in GridEditAreas)
                {
                    editArea.Visibility = Visibility.Collapsed;
                }
                foreach (var editTextBlock in editTextBlocks)
                {
                    editTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CommentsDisplay_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.SizeChanged += OnSizeChanged;
                OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs(selectedShow.ShowType == ShowType.Movie ? FirestoreManager.MovieComments:FirestoreManager.TvShowComments));
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                var comments = selectedShow.ShowType == ShowType.Movie
                    ? FirestoreManager.MovieComments
                    : FirestoreManager.TvShowComments;
                for (int i = 0; i < comments.Count; i++)
                {
                    var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    var currentMainComment = CommentsDisplay.Items[i] as Comment;
                    if (currentMainComment == null) continue;
              
                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            StackPanel stackPanelTextMain = FindDescendantByName<StackPanel>(lbi, "StackPanelTExtMain");
                            if (stackPanelTextMain != null)
                            {
                                stackPanelTextMain.Width = CommentsDisplay.ActualWidth - 300;
                            }

                            ListBox replyListBox = FindDescendantByName<ListBox>(lbi, "ReplyCommentsDisplay");
                            if (replyListBox != null && currentMainComment.ReplyComments != null)
                            {
                                for (int z = 0; z < currentMainComment.ReplyComments.Count; z++)
                                {
                                    var lbiReply = replyListBox.ItemContainerGenerator.ContainerFromIndex(z) as ListBoxItem;
                                    if (lbiReply == null) continue;
                                    var currentReplyComment = replyListBox.Items[z] as Comment;
                                    if (currentReplyComment == null) continue;
                              

                                    StackPanel stackPanelTextReply = FindDescendantByName<StackPanel>(lbiReply, "StackPanelTExtReply");
                                    if (stackPanelTextReply != null)
                                    {
                                        stackPanelTextReply.Width = CommentsDisplay.ActualWidth - 300;
                                    }
                                }
                            }

                        }));
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
    }

    public class OnItemLoadingFinishedEventArgs : EventArgs
    {
        public AsyncObservableCollection<Comment> comments { get; set; }

        public OnItemLoadingFinishedEventArgs(AsyncObservableCollection<Comment> comments)
        {
            this.comments = comments;
        }
    }
}
