using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material.Icons.Avalonia;
using Serilog;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace NetStream.Views;

public partial class CommentPage : UserControl,IDisposable
{
    public CommentPage()
    {
        InitializeComponent();
    }
    
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

    private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
    {
        Console.WriteLine("InstanceOnSizeChanged");
        ApplyResponsiveLayout(e.width);
    }


    private ObservableCollection<Comment> commentLists = new ObservableCollection<Comment>();
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
              await Dispatcher.UIThread.InvokeAsync(() =>
              {
                      Expander expander = FindDescendantByName<Expander>(lbi, "ExpanderReplyComments");
                      if (expander != null)
                      {
                          if (currentMainComment.ReplyComments == null || currentMainComment.ReplyComments.Count == 0)
                          {
                              expander.IsVisible = false;
                          }
                          else
                          {
                              expander.IsVisible = true;
                          }
                      }

                      // Ana yorumlar için 3 nokta ikonunun görünürlüğünü ayarla
                      MaterialIcon menuIcon = FindDescendantByName<MaterialIcon>(lbi, "MenuIconMainComment");
                      if (menuIcon != null)
                      {
                          menuIcon.IsVisible = currentMainComment.Email == AppSettingsManager.appSettings.FireStoreEmail;
                      }
                     

                      StackPanel stackPanelTextMain = FindDescendantByName<StackPanel>(lbi, "StackPanelTExtMain");
                      if (stackPanelTextMain != null)
                      {
                          stackPanelTextMain.Width = CommentsDisplay.Bounds.Width - 300;
                      }

                      MaterialIcon iconBlockThumbsUp = FindDescendantByName<MaterialIcon>(lbi, "ThumbsUpMainComment");
                      if (iconBlockThumbsUp != null)
                      {
                          iconBlockThumbsUp.Foreground = currentMainComment.LikedByMe
                              ? new SolidColorBrush(Colors.White)
                              : new SolidColorBrush(Color.FromArgb(255, 145, 145, 152));
                      }

                      MaterialIcon iconBlockThumbsDown = FindDescendantByName<MaterialIcon>(lbi, "ThumbsDownMainComment");
                      if (iconBlockThumbsDown != null)
                      {
                          iconBlockThumbsDown.Foreground = currentMainComment.DislikedByMe
                              ? new SolidColorBrush(Colors.White)
                              : new SolidColorBrush(Color.FromArgb(255, 145, 145, 152));
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
                              // Yanıt yorumları için 3 nokta ikonunun görünürlüğünü ayarla
                              MaterialIcon replyMenuIcon = FindDescendantByName<MaterialIcon>(lbiReply, "MenuIconReplyComment");
                              if (replyMenuIcon != null)
                              {
                                  replyMenuIcon.IsVisible = currentReplyComment.Email == AppSettingsManager.appSettings.FireStoreEmail;
                              }


                              StackPanel stackPanelTextReply = FindDescendantByName<StackPanel>(lbiReply, "StackPanelTExtReply");
                              if (stackPanelTextReply != null)
                              {
                                  stackPanelTextReply.Width = CommentsDisplay.Bounds.Width - 300;
                              }

                              MaterialIcon iconBlockThumbsUpReplyComment = FindDescendantByName<MaterialIcon>(lbiReply, "ThumbsUpReplyComments");
                              if (iconBlockThumbsUpReplyComment != null)
                              {
                                  iconBlockThumbsUpReplyComment.Foreground = currentReplyComment.LikedByMe
                                      ? new SolidColorBrush(Colors.White)
                                      : new SolidColorBrush(Color.FromArgb(255, 145, 145, 152));
                              }

                              MaterialIcon iconBlockThumbsDownReplyComment = FindDescendantByName<MaterialIcon>(lbiReply, "ThumbsDownReplyComments");
                              if (iconBlockThumbsDownReplyComment != null)
                              {
                                  iconBlockThumbsDownReplyComment.Foreground = currentReplyComment.DislikedByMe
                                      ? new SolidColorBrush(Colors.White)
                                      : new SolidColorBrush(Color.FromArgb(255, 145, 145, 152));
                              }
                          }
                      }

                  });
          }

          await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
          {
                  this.TextBlockCommentCounter.Text = comments.Count + replyCommentCounter + " " + ResourceProvider.GetString("CommentsStringLowerCase");
          });
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
          ViewboxNoCommentsFound.IsVisible = false;
          SearchingPanel.IsVisible = true;
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

    private void CommentPage_OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            foreach (var openedReply in OpenedReplys)
            {
                openedReply.IsVisible = false;
            }
            foreach (var editArea in GridEditAreas)
            {
                editArea.IsVisible = false;
            }
            foreach (var editTextBlock in editTextBlocks)
            {
                editTextBlock.IsVisible = true;
            }
            CloseAllPopups();

        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private void CommentPage_OnUnloaded(object? sender, RoutedEventArgs e)
    {
       
    }

    private void CommentsDisplay_OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            OnItemLoadingFinished(new OnItemLoadingFinishedEventArgs(selectedShow.ShowType == ShowType.Movie ? FirestoreManager.MovieComments:FirestoreManager.TvShowComments));
            ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Console.WriteLine(errorMessage);
        }
    }
    

    // Responsive düzen için yeni method
    private void ApplyResponsiveLayout(double width)
    {
        try
        {
            var titleFontSize = CalculateTextSize(width, 18, 32);
            var counterFontSize = CalculateTextSize(width, 14, 22);
            var commentFontSize = CalculateTextSize(width, 14, 22);
            var buttonFontSize = CalculateTextSize(width, 14, 22);
            
            TextBlockCommentTitle.FontSize = titleFontSize;
            TextBlockCommentCounter.FontSize = counterFontSize;
            TextBlockCommentCounter.Margin = CalculateCounterMargin(width);
            
            var profileBorderSize = CalculateProfileSize(width);
            ProfileImageBorder.Width = profileBorderSize;
            ProfileImageBorder.Height = profileBorderSize;
            ProfileImageBorder.CornerRadius = new CornerRadius(profileBorderSize / 2);

            var leftMargin2 = CalculateScaledValue(width, 8, 16);
            var topMargin2 = CalculateScaledValue(width, 16, 24);
            CommentTextBox.FontSize = commentFontSize;
            CommentTextBox.Margin = new Thickness(leftMargin2,topMargin2, 0, 0);
           
            CommentSendBtn.FontSize = buttonFontSize;
            CommentSendBtn.Margin = new Thickness(leftMargin2,topMargin2, 0, 0); ;
            
            var leftMargin = width < 600 ? 5 : 25;
            CommentsDisplay.Margin = new Thickness(leftMargin, 0, 0, 0);
            
            NoCommentsFound.FontSize = CalculateTextSize(width, 16, 28);
            
            var comments = selectedShow.ShowType == ShowType.Movie
                ? FirestoreManager.MovieComments
                : FirestoreManager.TvShowComments;
            
            if (comments != null && comments.Count > 0)
            {
                ApplyResponsiveCommentStyles(width, comments);
            }
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error in ApplyResponsiveLayout: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Console.WriteLine(errorMessage);
        }
    }

    // Dinamik metin boyutu hesaplama
    private double CalculateTextSize(double width, double minSize, double maxSize)
    {
        // Responsive tasarım için daha hassas font boyutu hesaplaması
        double baseValue = CalculateScaledValue(width, minSize, maxSize);
        
        // Çok küçük ekranlarda minimum değer koruması
        if (width < 600) 
        {
            return Math.Max(minSize, baseValue * 0.9);
        }
        // Çok büyük ekranlarda maximum değer koruması
        else if (width > 2500)
        {
            return Math.Min(maxSize, baseValue * 1.05);
        }
        
        return baseValue;
    }

    // Sayaçlar için margin hesaplama metodu
    private Thickness CalculateCounterMargin(double width)
    {
        double left = CalculateScaledValue(width, 6, 16);
        double top = CalculateScaledValue(width, 4, 9);
        
        return new Thickness(left, top, 0, 0);
    }

    // Profil resim boyutu hesaplama
    private double CalculateProfileSize(double width)
    {
        return CalculateScaledValue(width, 50, 75);
    }

    // Ölçekleme hesaplama için yardımcı metot
    private double CalculateScaledValue(double width, double minValue, double maxValue)
    {
        const double minWidth = 320;   // En küçük ekran genişliği (piksel)
        const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            
        double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
        double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
        double scaledValue = minValue + scale * (maxValue - minValue);
            
        return Math.Round(scaledValue);
    }

    private void PopupBOxMainCommentEditBtnPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if(button == null) return;
            
            var popup = FindAncestor<Popup>(button);
            if (popup != null)
            {
                popup.IsOpen = false;
            }
            
            // Butonun bağlamından yorumu al
            var comment = button.DataContext as Comment;
            if (comment == null)
            {
                // Butonun doğrudan DataContext'i yoksa, Popup veya parent konteynerden almayı dene
                if (popup != null && popup.DataContext is Comment popupComment)
                {
                    comment = popupComment;
                }
                else
                {
                    var listBoxItem = FindAncestor<ListBoxItem>(button);
                    if (listBoxItem != null && listBoxItem.DataContext is Comment listBoxComment)
                    {
                        comment = listBoxComment;
                    }
                }
                
                if (comment == null) return;
            }

            // Ana yorum için ListBoxItem'ı bul
            var mainComments = selectedShow.ShowType == ShowType.Movie
                ? FirestoreManager.MovieComments
                : FirestoreManager.TvShowComments;
            
            selectedIndex = -1;
            for (int i = 0; i < mainComments.Count; i++)
            {
                if (mainComments[i].Id == comment.Id)
                {
                    selectedIndex = i;
                    break;
                }
            }
            
            if (selectedIndex == -1) return;

            var selectedMainCommentListBoxItem = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
            if (selectedMainCommentListBoxItem == null) return;

            // Tüm açık olan edit alanlarını ve reply alanlarını kapat
            foreach (var editArea in GridEditAreas)
            {
                editArea.IsVisible = false;
            }
            foreach (var editTextBlock in editTextBlocks)
            {
                editTextBlock.IsVisible = true;
            }
            foreach (var openedReply in OpenedReplys)
            {
                openedReply.IsVisible = false;
            }
            
            // Edit alanını bulup aç
            Grid gridEditArea = FindDescendantByName<Grid>(selectedMainCommentListBoxItem, "GridEditArea");
            if (gridEditArea == null) return;
            gridEditArea.Width =  MainView.Instance.Bounds.Width *0.8;
            gridEditArea.IsVisible = true;
            GridEditAreas.Add(gridEditArea);
            
            // TextBox ve TextBlock referanslarını al
            TextBox textBoxReplyEdit = FindDescendantByName<TextBox>(selectedMainCommentListBoxItem, "MainCommentTextBoxEditReply");
            TextBlock TextBlockMainCommentText = FindDescendantByName<TextBlock>(selectedMainCommentListBoxItem, "TextBlockMainCommentText");
            EditSaveButton = FindDescendantByName<Button>(selectedMainCommentListBoxItem, "MainCommentEditReplySendBtn");
            
            if (textBoxReplyEdit == null || TextBlockMainCommentText == null) return;
            
            // TextBlock'u gizle ve mevcut yorumu TextBox'a kopyala
            TextBlockMainCommentText.IsVisible = false;
            editTextBlocks.Add(TextBlockMainCommentText);
            textBoxReplyEdit.Text = comment.Text;
            
            // Kaydet butonunu devre dışı bırak ve text değiştiğinde etkinleştir
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

    private void TextBoxReplyEditOnTextChanged(object? sender, TextChangedEventArgs e)
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

    private async void PopupBOxMainCommentDeleteBtnPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button == null) return;
            
            var popup = FindAncestor<Popup>(button);
            if (popup != null)
            {
                popup.IsOpen = false;
            }
            
            // Butonun bağlamından yorumu al
            var comment = button.DataContext as Comment;
            if (comment == null)
            {
                // Butonun doğrudan DataContext'i yoksa, Popup veya parent konteynerden almayı dene
                if (popup != null && popup.DataContext is Comment popupComment)
                {
                    comment = popupComment;
                }
                else
                {
                    var listBoxItem = FindAncestor<ListBoxItem>(button);
                    if (listBoxItem != null && listBoxItem.DataContext is Comment listBoxComment)
                    {
                        comment = listBoxComment;
                    }
                }
                
                if (comment == null) return;
            }
            
            // Yorumu sil
            await FirestoreManager.DeleteComment(selectedShow.Id, selectedShow.ShowType, comment.Id, this);
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }
    private Button EditSaveButton;

    private async void MainCommentEditReplySendBtnPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        try
        {
            var selectedMainCommentListBoxItem = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
            var selectedMainComment = CommentsDisplay.Items[selectedIndex] as Comment;
            if (selectedMainCommentListBoxItem == null || selectedMainComment == null) return;

            Grid gridEditArea = FindDescendantByName<Grid>(selectedMainCommentListBoxItem, "GridEditArea");
            if (gridEditArea == null) return;
            gridEditArea.IsVisible = false;
            TextBox textBoxReplyEdit = FindDescendantByName<TextBox>(selectedMainCommentListBoxItem, "MainCommentTextBoxEditReply");
            TextBlock TextBlockMainCommentText = FindDescendantByName<TextBlock>(selectedMainCommentListBoxItem, "TextBlockMainCommentText");
            if (textBoxReplyEdit == null || TextBlockMainCommentText == null) return;
            TextBlockMainCommentText.IsVisible = true;
            await FirestoreManager.EditComment(selectedShow.Id, selectedShow.ShowType, selectedMainComment.Id,
                textBoxReplyEdit.Text);
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private void MainCommentCancelEditReplySendBtnPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var editArea in GridEditAreas)
            {
                editArea.IsVisible = false;
            }
            foreach (var editTextBlock in editTextBlocks)
            {
                editTextBlock.IsVisible = true;
            }
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private async void ThumbsUpMainCOmmentPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        try
        {
            var icon = sender as MaterialIcon;
            if (icon == null) return;
            
            // Icon'un bağlamından yorumu al
            var comment = icon.DataContext as Comment;
            if (comment == null) return;
            
            // Yorumun ID'si ile liste içindeki konumunu bul
            var comments = selectedShow.ShowType == ShowType.Movie
                ? FirestoreManager.MovieComments
                : FirestoreManager.TvShowComments;
            
            selectedIndex = -1;
            for (int i = 0; i < comments.Count; i++)
            {
                if (comments[i].Id == comment.Id)
                {
                    selectedIndex = i;
                    break;
                }
            }
            
            if (selectedIndex == -1) return;
            
            // Beğen işlemini gerçekleştir
            await FirestoreManager.LikeDislikeComment(selectedShow.Id, selectedShow.ShowType, InteractionType.Like, comment.Id, this);
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private async void ThumbsDownMainCOmmentPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        try
        {
            var icon = sender as MaterialIcon;
            if (icon == null) return;
            
            // Icon'un bağlamından yorumu al
            var comment = icon.DataContext as Comment;
            if (comment == null) return;
            
            // Yorumun ID'si ile liste içindeki konumunu bul
            var comments = selectedShow.ShowType == ShowType.Movie
                ? FirestoreManager.MovieComments
                : FirestoreManager.TvShowComments;
            
            selectedIndex = -1;
            for (int i = 0; i < comments.Count; i++)
            {
                if (comments[i].Id == comment.Id)
                {
                    selectedIndex = i;
                    break;
                }
            }
            
            if (selectedIndex == -1) return;
            
            // Beğenme işlemini gerçekleştir
            await FirestoreManager.LikeDislikeComment(selectedShow.Id, selectedShow.ShowType, InteractionType.Dislike, comment.Id, this);
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }
    
    private async void ReplyButtonPreviewMouseLeftDown(object? sender, PointerPressedEventArgs e)
{
    try
    {
        // Doğrudan tıklanan TextBlock ve parent Comment
        var replyTextBlock = sender as TextBlock;
        if (replyTextBlock == null) return;
        
        var comment = replyTextBlock.DataContext as Comment;
        if (comment == null) return;
        
        // Önce tüm açık alanları kapat
        foreach (var openedReply in OpenedReplys)
        {
            openedReply.IsVisible = false;
        }
        foreach (var editArea in GridEditAreas)
        {
            editArea.IsVisible = false;
        }
        foreach (var editTextBlock in editTextBlocks)
        {
            editTextBlock.IsVisible = true;
        }
        
        // Tıklanan comment'i bul
        var comments = selectedShow.ShowType == ShowType.Movie
            ? FirestoreManager.MovieComments
            : FirestoreManager.TvShowComments;
        
        selectedIndex = -1;
        for (int i = 0; i < comments.Count; i++)
        {
            if (comments[i].Id == comment.Id)
            {
                selectedIndex = i;
                break;
            }
        }
        
        if (selectedIndex == -1) return;
        
        // ListBoxItem'ı bul
        var selectedListboxItem = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
        if (selectedListboxItem == null) return;
            
        // Grid'i bulmak için tam hiyerarşi adını dene
        Grid gridReply = null;
        
        // Önce standart metotla dene
        gridReply = FindDescendantByName<Grid>(selectedListboxItem, "GridReplyArea");
        
        // Bulunamazsa, logla ve tüm grid'leri kontrol et
        if (gridReply == null)
        {
            Console.WriteLine("GridReplyArea bulunamadı, tüm VisualTree taranıyor...");
            // Tüm visual tree'yi tara ve grid'leri bul
            var allVisuals = TraverseVisualTree(selectedListboxItem);
            foreach (var visual in allVisuals)
            {
                if (visual is Grid grid)
                {
                    // İsim hiyerarşisini logla
                    var name = grid.Name;
                    Console.WriteLine($"Grid bulundu: {name}");
                    
                    // Eğer "Reply" veya "reply" kelimesi içeriyorsa
                    if (name != null && name.Contains("Reply", StringComparison.OrdinalIgnoreCase) && 
                        name.Contains("Area", StringComparison.OrdinalIgnoreCase))
                    {
                        gridReply = grid;
                        break;
                    }
                }
            }
        }
        
        if (gridReply == null)
        {
            Console.WriteLine("GridReplyArea bulunamadı, işlem iptal edildi");
            return;
        }
        
        // Grid'i gör ve listede tut
        gridReply.IsVisible = true;
       // gridReply.Width = CommentsDisplay.Bounds.Width - 700;
        OpenedReplys.Add(gridReply);

        // Profil resmini yükle
        Image ProfileImageReply = FindDescendantByName<Image>(selectedListboxItem, "ProfileImageReply");
        if (ProfileImageReply != null)
        {
            ProfileImageReply.Source = await FirestoreManager.DownloadProfilePhoto(
                AppSettingsManager.appSettings.FireStoreProfilePhotoName, true);
        }
    }
    catch (System.Exception exception)
    {
        var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
        Log.Error(errorMessage);
        Console.WriteLine(errorMessage);
    }
}

// Visual Tree'yi gezmek için yardımcı metot
private List<Visual> TraverseVisualTree(Visual parent)
{
    var result = new List<Visual>();
    var children = parent.GetVisualChildren();
    
    foreach (var child in children)
    {
        if (child is Visual visual)
        {
            result.Add(visual);
            result.AddRange(TraverseVisualTree(visual));
        }
    }
    
    return result;
}
    
  public T FindDescendantByName<T>(Visual obj, string objname) where T : Visual
  {
      try
      {
          // 1. Önce mevcut nesneyi kontrol et
          if (obj is T)
          {
              string controlName = "";
              var nameProperty = obj.GetType().GetProperty("Name");
              if (nameProperty != null)
              {
                  controlName = (string)nameProperty.GetValue(obj, null) ?? "";
                  if (string.Equals(controlName, objname, StringComparison.OrdinalIgnoreCase))
                  {
                      return (T)obj;
                  }
              }
          }

          // 2. Çocuk nesneleri kontrol et
          foreach (var child in obj.GetVisualChildren())
          {
              if (child is Visual visualChild)
              {
                  // 2a. Her çocuğu kontrol et
                  if (child is T)
                  {
                      string childName = "";
                      var nameProperty = child.GetType().GetProperty("Name");
                      if (nameProperty != null)
                      {
                          childName = (string)nameProperty.GetValue(child, null) ?? "";
                          if (string.Equals(childName, objname, StringComparison.OrdinalIgnoreCase))
                          {
                              return (T)child;
                          }
                      }
                  }
                  
                  // 2b. Recursive olarak bu çocuğun altındaki elemanları kontrol et
                  var result = FindDescendantByName<T>(visualChild, objname);
                  if (result != null)
                  {
                      return result;
                  }
              }
          }
      }
      catch (System.Exception e)
      {
          var errorMessage = $"Error in FindDescendantByName: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
          Log.Error(errorMessage);
          Console.WriteLine(errorMessage);
      }

      return null;
  }

  private static bool IsMouseOverTarget(Visual target, Avalonia.Point point)
  {
      var bounds = target.Bounds;
      return bounds.Contains(point);
  }

    private async void CommentSendReplyButtonPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
        if (button == null) 
        {
            return;
        }
        
        // selectedIndex doğru mu kontrol et
        if (selectedIndex == -1) 
        {
            return;
        }
        
        var selectedComment = CommentsDisplay.Items[selectedIndex] as Comment;
        var selectedListboxItem = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
        if (selectedComment == null || selectedListboxItem == null)
        {
            return;
        }

        var parentGrid = FindAncestor<Grid>(button);
        Grid replyArea = null;
        
        if (parentGrid != null && parentGrid.Name?.Contains("Reply") == true)
        {
            replyArea = parentGrid;
        }
        else
        {
            replyArea = FindDescendantByName<Grid>(selectedListboxItem, "GridReplyArea");
        }
        
        if (replyArea == null)
        {
            var allVisuals = TraverseVisualTree(selectedListboxItem);
            foreach (var visual in allVisuals)
            {
                if (visual is Grid grid && grid.Name?.Contains("Reply") == true)
                {
                    Log.Debug($"Found grid: {grid.Name}");
                    replyArea = grid;
                    break;
                }
            }
        }
        
        if (replyArea == null)
        {
            return;
        }
        
        // Grid içindeki TextBox'ı bul
        TextBox textBoxReply = null;
        
        // Önce doğrudan normal metot ile ara
        textBoxReply = FindDescendantByName<TextBox>(replyArea, "CommentTextBoxReply");
        
        // Bulunamazsa, Grid'deki tüm TextBox'ları kontrol et
        if (textBoxReply == null)
        {
            var children = TraverseVisualTree(replyArea);
            
            foreach (var child in children)
            {
                if (child is TextBox textBox)
                {
                    textBoxReply = textBox; // İlk bulunan TextBox'ı kullan
                    break;
                }
            }
        }

        if (textBoxReply == null)
        {
            return;
        }

        if (String.IsNullOrWhiteSpace(textBoxReply.Text))
        {
            LocalDialogManager.ShowDialog("NetStream",ResourceProvider.GetString("ReplyCommentEmptyError"),NotificationType.Error);
        }
        else
        {
            // Yanıt gönder
            await FirestoreManager.AddComment(
                selectedShow.Id,
                selectedShow.ShowType, 
                "@" + selectedComment.DisplayName + " " + textBoxReply.Text, 
                selectedComment.Id,
                this);
                
            textBoxReply.Text = "";
            
            // Tüm açık yanıt alanlarını kapat
            foreach (var openedReply in OpenedReplys)
            {
                openedReply.IsVisible = false;
            }
        }
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }
    
    private T FindAncestor<T>(Visual element) where T : Visual
    {
        var parent = element.GetVisualParent();
    
        if (parent == null)
            return null;
        
        if (parent is T tParent)
            return tParent;
        
        return FindAncestor<T>(parent);
    }
    
    private int selectedIndex = -1;
    private List<Grid> OpenedReplys = new List<Grid>();

    private void ReplyCommentsDisplayLoaded(object? sender, RoutedEventArgs e)
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
                 stackPanelTextReply.Width = CommentsDisplay.Bounds.Width - 300;
             }
         }
         for (int z = 0; z < currentMainComment.ReplyComments.Count; z++)
         {
             var lbiReply = replyListBox.ItemContainerGenerator.ContainerFromIndex(z) as ListBoxItem;
             if (lbiReply == null) continue;
             var currentReplyComment = replyListBox.Items[z] as Comment;
             if (currentReplyComment == null) continue;
             Popup popupBoxReplyComment = FindDescendantByName<Popup>(lbiReply, "PopupBoxReplyComment");
             if (popupBoxReplyComment != null)
             {
                 if (currentReplyComment.Email == AppSettingsManager.appSettings.FireStoreEmail)
                 {
                     popupBoxReplyComment.IsVisible = true;
                 }
                 else
                 {
                     popupBoxReplyComment.IsVisible = false;
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

    private void PopupBOxReplyCommentEditBtnPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button == null) return;
            
            var popup = FindAncestor<Popup>(button);
            if (popup != null)
            {
                popup.IsOpen = false;
            }
            
            // Butonun bağlamından yorumu al
            var comment = button.DataContext as Comment;
            if (comment == null)
            {
                // Butonun doğrudan DataContext'i yoksa, Popup veya parent konteynerden almayı dene
                if (popup != null && popup.DataContext is Comment popupComment)
                {
                    comment = popupComment;
                }
                else
                {
                    var listBoxItem = FindAncestor<ListBoxItem>(button);
                    if (listBoxItem != null && listBoxItem.DataContext is Comment listBoxComment)
                    {
                        comment = listBoxComment;
                    }
                }
                
                if (comment == null) return;
            }
            
            // Yanıt yorumunun olduğu ana yorumu bul
            var mainComments = selectedShow.ShowType == ShowType.Movie
                ? FirestoreManager.MovieComments
                : FirestoreManager.TvShowComments;
            
            selectedIndex = -1;
            replyListboxSelectedIndex = -1;
            
            // Önce ana yorumu ve yanıt yorumunu bul
            for (int i = 0; i < mainComments.Count; i++)
            {
                var mainComment = mainComments[i];
                if (mainComment.ReplyComments != null)
                {
                    for (int j = 0; j < mainComment.ReplyComments.Count; j++)
                    {
                        if (mainComment.ReplyComments[j].Id == comment.Id)
                        {
                            selectedIndex = i;
                            replyListboxSelectedIndex = j;
                            break;
                        }
                    }
                }
                
                if (selectedIndex != -1) break;
            }
            
            if (selectedIndex == -1 || replyListboxSelectedIndex == -1) return;

            // Ana yorum listbox item'ını bul
            var selectedMainCommentListBoxItem = this.CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
            if (selectedMainCommentListBoxItem == null) return;

            // Yanıt yorumları listbox'ını bul
            ListBox replyListBox = FindDescendantByName<ListBox>(selectedMainCommentListBoxItem, "ReplyCommentsDisplay");
            if (replyListBox == null) return;
            
            // Yanıt yorumu listbox item'ını bul
            var selectedReplyCommentListboxItem = replyListBox.ItemContainerGenerator.ContainerFromIndex(replyListboxSelectedIndex) as ListBoxItem;
            var selectedReplyCommentTExt = replyListBox.Items[replyListboxSelectedIndex] as Comment;
            if (selectedReplyCommentListboxItem == null || selectedReplyCommentTExt == null) return;

            // Tüm açık olan edit alanlarını ve reply alanlarını kapat
            foreach (var editArea in GridEditAreas)
            {
                editArea.IsVisible = false;
            }
            foreach (var editTextBlock in editTextBlocks)
            {
                editTextBlock.IsVisible = true;
            }
            foreach (var openedReply in OpenedReplys)
            {
                openedReply.IsVisible = false;
            }

            // Edit alanını bulup aç
            Grid gridEditArea = FindDescendantByName<Grid>(selectedReplyCommentListboxItem, "GridEditReplyArea");
            if (gridEditArea == null) return;
            gridEditArea.Width =  MainView.Instance.Bounds.Width *0.8;
            gridEditArea.IsVisible = true;
            GridEditAreas.Add(gridEditArea);
            
            // TextBox ve TextBlock referanslarını al
            TextBox textBoxReplyEdit = FindDescendantByName<TextBox>(selectedReplyCommentListboxItem, "ReplyCommentTextBoxEditReply");
            TextBlock TextBlockMainCommentText = FindDescendantByName<TextBlock>(selectedReplyCommentListboxItem, "TextBlockTextReplyComment");
            EditSaveButton = FindDescendantByName<Button>(selectedReplyCommentListboxItem, "ReplyCommentEditReplySendBtn");
            
            if (textBoxReplyEdit == null || TextBlockMainCommentText == null) return;
            
            // TextBlock'u gizle ve mevcut yorumu TextBox'a kopyala
            TextBlockMainCommentText.IsVisible = false;
            editTextBlocks.Add(TextBlockMainCommentText);
            textBoxReplyEdit.Text = selectedReplyCommentTExt.Text;
            
            // Kaydet butonunu devre dışı bırak ve text değiştiğinde etkinleştir
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

    private async void PopupBOxReplyCommentDeleteBtnPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button == null) return;
            
            var popup = FindAncestor<Popup>(button);
            if (popup != null)
            {
                popup.IsOpen = false;
            }
            
            // Butonun bağlamından yorumu al
            var comment = button.DataContext as Comment;
            if (comment == null)
            {
                // Butonun doğrudan DataContext'i yoksa, Popup veya parent konteynerden almayı dene
                if (popup != null && popup.DataContext is Comment popupComment)
                {
                    comment = popupComment;
                }
                else
                {
                    var listBoxItem = FindAncestor<ListBoxItem>(button);
                    if (listBoxItem != null && listBoxItem.DataContext is Comment listBoxComment)
                    {
                        comment = listBoxComment;
                    }
                }
                
                if (comment == null) return;
            }
            
            // Yorumu sil
            await FirestoreManager.DeleteComment(selectedShow.Id, selectedShow.ShowType, comment.Id, this);
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private async void ReplyCommentEditReplySendBtnPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
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
            gridEditArea.IsVisible = false;
            TextBox textBoxReplyEdit = FindDescendantByName<TextBox>(selectedReplyCommentListBoxItem, "ReplyCommentTextBoxEditReply");
            TextBlock TextBlockMainCommentText = FindDescendantByName<TextBlock>(selectedReplyCommentListBoxItem, "TextBlockTextReplyComment");
            if (textBoxReplyEdit == null || TextBlockMainCommentText == null) return;
            TextBlockMainCommentText.IsVisible = true;
            await FirestoreManager.EditComment(selectedShow.Id, selectedShow.ShowType, selectedReplyComment.Id,
                textBoxReplyEdit.Text);
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }   
    }

    private void ReplyCommentCancelEditReplySendBtnPreviewMouseLeftDown(object? sender, RoutedEventArgs e)
    {
        
        try
        {
            foreach (var editArea in GridEditAreas)
            {
                editArea.IsVisible = false;
            }
            foreach (var editTextBlock in editTextBlocks)
            {
                editTextBlock.IsVisible = true;
            }
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private async void ThumsUpReplyCommentsPreviewMouseLeftDown(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            var icon = sender as MaterialIcon;
            if (icon == null) return;
            
            // Icon'un bağlamından yorumu al
            var replyComment = icon.DataContext as Comment;
            if (replyComment == null) return;
            
            // Ana yorumu ve yanıt yorumunu bul
            var comments = selectedShow.ShowType == ShowType.Movie
                ? FirestoreManager.MovieComments
                : FirestoreManager.TvShowComments;
            
            selectedIndex = -1;
            replyListboxSelectedIndex = -1;
            
            // Yanıt yorumunun ID'si ile ana yorumu ve yanıt yorumunu bul
            for (int i = 0; i < comments.Count; i++)
            {
                var mainComment = comments[i];
                if (mainComment.ReplyComments != null)
                {
                    for (int j = 0; j < mainComment.ReplyComments.Count; j++)
                    {
                        if (mainComment.ReplyComments[j].Id == replyComment.Id)
                        {
                            selectedIndex = i;
                            replyListboxSelectedIndex = j;
                            break;
                        }
                    }
                }
                
                if (selectedIndex != -1) break;
            }
            
            if (selectedIndex == -1 || replyListboxSelectedIndex == -1) return;
            
            // Beğen işlemini gerçekleştir
            await FirestoreManager.LikeDislikeComment(selectedShow.Id, selectedShow.ShowType, InteractionType.Like, replyComment.Id, this);
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private async void ThumsDownReplyCommentsPreviewMouseLeftDown(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            var icon = sender as MaterialIcon;
            if (icon == null) return;
            
            // Icon'un bağlamından yorumu al
            var replyComment = icon.DataContext as Comment;
            if (replyComment == null) return;
            
            // Ana yorumu ve yanıt yorumunu bul
            var comments = selectedShow.ShowType == ShowType.Movie
                ? FirestoreManager.MovieComments
                : FirestoreManager.TvShowComments;
            
            selectedIndex = -1;
            replyListboxSelectedIndex = -1;
            
            // Yanıt yorumunun ID'si ile ana yorumu ve yanıt yorumunu bul
            for (int i = 0; i < comments.Count; i++)
            {
                var mainComment = comments[i];
                if (mainComment.ReplyComments != null)
                {
                    for (int j = 0; j < mainComment.ReplyComments.Count; j++)
                    {
                        if (mainComment.ReplyComments[j].Id == replyComment.Id)
                        {
                            selectedIndex = i;
                            replyListboxSelectedIndex = j;
                            break;
                        }
                    }
                }
                
                if (selectedIndex != -1) break;
            }
            
            if (selectedIndex == -1 || replyListboxSelectedIndex == -1) return;
            
            // Beğenmeme işlemini gerçekleştir
            await FirestoreManager.LikeDislikeComment(selectedShow.Id, selectedShow.ShowType, InteractionType.Dislike, replyComment.Id, this);
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }
    public event EventHandler<OnItemLoadingFinishedEventArgs> OnItemsLoaded;

    private int replyListboxSelectedIndex;
    
    public virtual void OnItemLoadingFinished(OnItemLoadingFinishedEventArgs e)
    {
        OnItemsLoaded?.Invoke(this, e);
    }
    private List<Grid> GridEditAreas = new List<Grid>();
    private List<TextBlock> editTextBlocks = new List<TextBlock>();
    private string textBeforeEdit = "";

    private async void ReplyButton2PreviewMouseleftDown(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            // Doğrudan tıklanan TextBlock ve parent Comment
            var replyTextBlock = sender as TextBlock;
            if (replyTextBlock == null) return;
            
            var replyComment = replyTextBlock.DataContext as Comment;
            if (replyComment == null) return;
            
            // Önce tüm açık alanları kapat
            foreach (var openedReply in OpenedReplys)
            {
                openedReply.IsVisible = false;
            }
            foreach (var editArea in GridEditAreas)
            {
                editArea.IsVisible = false;
            }
            foreach (var editTextBlock in editTextBlocks)
            {
                editTextBlock.IsVisible = true;
            }
            
            // Ana yorumu ve yanıt yorumunu bul
            var comments = selectedShow.ShowType == ShowType.Movie
                ? FirestoreManager.MovieComments
                : FirestoreManager.TvShowComments;
            
            // Ana yorumu bul
            selectedIndex = -1;
            replyListboxSelectedIndex = -1;
            
            for (int i = 0; i < comments.Count; i++)
            {
                var mainComment = comments[i];
                if (mainComment.ReplyComments != null)
                {
                    for (int j = 0; j < mainComment.ReplyComments.Count; j++)
                    {
                        if (mainComment.ReplyComments[j].Id == replyComment.Id)
                        {
                            selectedIndex = i;
                            replyListboxSelectedIndex = j;
                            break;
                        }
                    }
                }
                
                if (selectedIndex != -1) break;
            }
            
            if (selectedIndex == -1 || replyListboxSelectedIndex == -1) 
            {
                return;
            }
            
            // Ana yorum ListBoxItem'ını bul
            var mainCommentListBoxItem = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
            if (mainCommentListBoxItem == null) 
            {
                return;
            }
            
            // ReplyCommentsDisplay ListBox'ını bul
            var replyListBox = FindDescendantByName<ListBox>(mainCommentListBoxItem, "ReplyCommentsDisplay");
            if (replyListBox == null) 
            {
                return;
            }
            
            // Yanıt yorumu ListBoxItem'ını bul
            var replyCommentListBoxItem = replyListBox.ItemContainerGenerator.ContainerFromIndex(replyListboxSelectedIndex) as ListBoxItem;
            if (replyCommentListBoxItem == null) 
            {
                return;
            }
            
            
            Grid gridReply = null;


            var allVisuals = TraverseVisualTree(replyCommentListBoxItem);
            foreach (var visual in allVisuals)
            {
                if (visual is Grid grid)
                {
                    var name = grid.Name;

                    if (name != null && (name.Contains("Reply2", StringComparison.OrdinalIgnoreCase) ||
                                         name.Contains("GridReply2", StringComparison.OrdinalIgnoreCase)) &&
                        name.Contains("Area", StringComparison.OrdinalIgnoreCase))
                    {
                        gridReply = grid;
                        break;
                    }
                }
            }
            
            
            if (gridReply == null)
            {
                return;
            }
            
            gridReply.IsVisible = true;
            //gridReply.Width = CommentsDisplay.Bounds.Width - 700;
            OpenedReplys.Add(gridReply);
            

            Image profileImageReply2 = null;
            
          
                var allGridChildren = TraverseVisualTree(gridReply);
                foreach (var child in allGridChildren)
                {
                    if (child is Image image)
                    {
                        profileImageReply2 = image;
                        break;
                    }
                }
            
            
            if (profileImageReply2 != null)
            {
                profileImageReply2.Source = await FirestoreManager.DownloadProfilePhoto(
                    AppSettingsManager.appSettings.FireStoreProfilePhotoName, true);
            }
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
            Console.WriteLine(errorMessage);
        }
    }

    private async void CommentSendBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(CommentTextBox.Text))
            {
                LocalDialogManager.ShowDialog("NetStream",ResourceProvider.GetString("CommentEmptyError"),NotificationType.Error);
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

    private async void CommentReplySendBtn2_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if(button == null) 
            {
                return;
            }
            
            if (selectedIndex == -1 || replyListboxSelectedIndex == -1) 
            {
                return;
            }
            
            // Ana yorumu bul
            var selectedMainComment = CommentsDisplay.Items[selectedIndex] as Comment;
            var selectedListboxItem = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
            if (selectedMainComment == null || selectedListboxItem == null)
            {
                return;
            }

            // ReplyListBox'u bul
            ListBox replyListBox = FindDescendantByName<ListBox>(selectedListboxItem, "ReplyCommentsDisplay");
            if (replyListBox == null)
            {
                return;
            }
            
            // Yanıt yorumunun ListBoxItem'ini bul
            var replyListBoxItem = replyListBox.ItemContainerGenerator.ContainerFromIndex(replyListboxSelectedIndex) as ListBoxItem;
            if (replyListBoxItem == null) 
            {
                return;
            }

            // Yanıt yorumunun Comment nesnesini bul
            var selectedReplyComment = replyListBox.Items[replyListboxSelectedIndex] as Comment;
            if(selectedReplyComment == null) 
            {
                return;
            }
            
            TextBox textBoxReply2 = null;
            
            var parentGrid = FindAncestor<Grid>(button);
            if (parentGrid != null)
            {
                var gridChildren = TraverseVisualTree(parentGrid);
                foreach (var child in gridChildren)
                {
                    if (child is TextBox textBox)
                    {
                        textBoxReply2 = textBox;
                        break;
                    }
                }
            }
            else
            {
               return;
            }
            
            if (textBoxReply2 == null)
            {
                return;
            }
            
            if (String.IsNullOrWhiteSpace(textBoxReply2.Text))
            {
                LocalDialogManager.ShowDialog("NetStream", ResourceProvider.GetString("ReplyCommentEmptyError"), NotificationType.Error);
            }
            else
            {
                var replyPrefix = $"@{selectedReplyComment.DisplayName} ";
                
                await FirestoreManager.AddComment(
                    selectedShow.Id,
                    selectedShow.ShowType, 
                    replyPrefix + textBoxReply2.Text, 
                    selectedMainComment.Id, 
                    this);
                
                textBoxReply2.Text = "";
                
                foreach (var openedReply in OpenedReplys)
                {
                    openedReply.IsVisible = false;
                }
                
            }
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error in CommentReplySendBtn2_OnClick: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
            Console.WriteLine(errorMessage);
        }
    }

    private void PopupBOxReplyCommentEditBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }

    private void CommentReplySendBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }

     private void CloseAllPopups()
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
                
                // Ana yorum popup'ını kapat
                var popupBox = FindDescendantByName<Popup>(lbi, "PopupBoxMainComment");
                if (popupBox != null)
                {
                    popupBox.IsOpen = false;
                }
                
                // Yanıt yorumların popup'larını kapat
                var currentMainComment = comments[i];
                if (currentMainComment.ReplyComments != null)
                {
                    ListBox replyListBox = FindDescendantByName<ListBox>(lbi, "ReplyCommentsDisplay");
                    if (replyListBox != null)
                    {
                        for (int j = 0; j < currentMainComment.ReplyComments.Count; j++)
                        {
                            var replyLbi = replyListBox.ItemContainerGenerator.ContainerFromIndex(j) as ListBoxItem;
                            if (replyLbi == null) continue;
                            
                            var replyPopupBox = FindDescendantByName<Popup>(replyLbi, "PopupBoxReplyComment");
                            if (replyPopupBox != null)
                            {
                                replyPopupBox.IsOpen = false;
                            }
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
    private void MenuIconMainComment_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            // Önce tüm popup'ları kapat
            CloseAllPopups();
            
            var icon = sender as MaterialIcon;
            if (icon == null) return;
            
            // Icon'un ListBoxItem'ını bul
            var parent = FindAncestor<ListBoxItem>(icon);
            if (parent == null) return;
            
            // Popup'u bul
            var popup = FindDescendantByName<Popup>(parent, "PopupBoxMainComment");
            if (popup == null) return;
            
            // Popup'u göster
            popup.PlacementTarget = icon;
            popup.IsOpen = true;
            
            // Olay işlendi
            e.Handled = true;
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private void MenuIconReplyComment_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            // Önce tüm popup'ları kapat
            CloseAllPopups();
            
            var icon = sender as MaterialIcon;
            if (icon == null) return;
            
            // Icon'un ListBoxItem'ını bul
            var parent = FindAncestor<ListBoxItem>(icon);
            if (parent == null) return;
            
            // Popup'u bul
            var popup = FindDescendantByName<Popup>(parent, "PopupBoxReplyComment");
            if (popup == null) return;
            
            // Popup'u göster
            popup.PlacementTarget = icon;
            popup.IsOpen = true;
            
            // Olay işlendi
            e.Handled = true;
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    // Yorum öğelerine responsive stil uygulama
    private async void ApplyResponsiveCommentStyles(double width, ObservableCollection<Comment> comments)
    {
        try
        {
           
            var avatarSize = CalculateProfileSize(width);
            var nameFontSize = CalculateTextSize(width, 12, 22);
            var dateFontSize = CalculateTextSize(width, 10, 17);
            var commentTextSize = CalculateTextSize(width, 12, 22);
            var actionButtonSize = CalculateTextSize(width, 12, 22);
            var iconSize = Math.Max(14, Math.Min(20, (int)(width / 70)));
            // Ana yorumlar için stil uygulaması
            for (int i = 0; i < comments.Count; i++)
            {
                var lbi = CommentsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (lbi == null) continue;
                
                await Dispatcher.UIThread.InvokeAsync(() => {
                    // Avatar boyutu
                    var avatar = FindDescendantByName<Border>(lbi, "GravatarBorder");
                    if (avatar != null)
                    {
                        avatar.Width = avatarSize;
                        avatar.Height = avatarSize;
                        avatar.CornerRadius = new CornerRadius(avatarSize / 2);
                        avatar.Margin = new Thickness(0, width < 600 ? 10 : 15, 0, 0);
                    }
                    
                    // Yorum metni için boyut ayarlama
                    var commentTextBlock = FindDescendantByName<TextBlock>(lbi, "TextBlockMainCommentText");
                    if (commentTextBlock != null)
                    {
                        commentTextBlock.FontSize = commentTextSize;
                        double maxWidth = 0;
                        if (width >= 1016 && width <= 1063)
                        {
                            maxWidth = width * 0.73;
                        }
                        else if (width >= 669 && width <= 740)
                        {
                            maxWidth = width * 0.6;
                        }
                        else if (width >= 633 && width <= 668)
                        {
                            maxWidth = width * 0.64;
                        }
                        else if (width >= 600 && width < 633)
                        {
                            maxWidth = width * 0.6;
                        }
                        else if (width <= 430)
                        {
                            maxWidth = width * 0.57;
                        }
                        else
                        {
                            maxWidth = width * 0.7;
                        }
                        commentTextBlock.MaxWidth = maxWidth;
                        // Küçük ekranlarda daha dar alan
                        var stackPanelTextMain = FindDescendantByName<StackPanel>(lbi, "StackPanelTExtMain");
                        if (stackPanelTextMain != null)
                        {
                            stackPanelTextMain.Width = maxWidth;
                        }
                    }
                    
                    // Kullanıcı adı ve tarih
                      var userDisplayNameTextBlock = FindDescendantByName<TextBlock>(lbi, "MainCommentDisplayName");
                      if (userDisplayNameTextBlock != null)
                      {
                          userDisplayNameTextBlock.FontSize = nameFontSize;
                      }
                      var relativeDateTextBlock = FindDescendantByName<TextBlock>(lbi, "MainCommentDate");
                      if (relativeDateTextBlock != null)
                      {
                          relativeDateTextBlock.FontSize = dateFontSize;
                          relativeDateTextBlock.Margin = new Thickness(CalculateScaledValue(width, 5, 10), 
                                                           CalculateScaledValue(width, 1, 4), 0, 0);
                      }
                      
                      // İkonlar
                      var thumbsUpIcon = FindDescendantByName<MaterialIcon>(lbi, "ThumbsUpMainComment");
                      var thumbsDownIcon = FindDescendantByName<MaterialIcon>(lbi, "ThumbsDownMainComment");
                      var menuIcon = FindDescendantByName<MaterialIcon>(lbi, "MenuIconMainComment");
                      
                      if (thumbsUpIcon != null)
                      {
                          thumbsUpIcon.Width = iconSize;
                          thumbsUpIcon.Height = iconSize;
                      }
                      
                      if (thumbsDownIcon != null)
                      {
                          thumbsDownIcon.Width = iconSize;
                          thumbsDownIcon.Height = iconSize;
                      }
                      
                      if (menuIcon != null)
                      {
                          menuIcon.Width = iconSize;
                          menuIcon.Height = iconSize;
                      }
                      
                      // Yanıt butonu
                      var replyButton = FindDescendantByName<TextBlock>(lbi, "ReplyButton");
                      if (replyButton != null)
                      {
                          replyButton.FontSize = actionButtonSize;
                      }
                      
                      // Yanıt için edit ve cevap alanları
                      var gridReplyArea = FindDescendantByName<Grid>(lbi, "GridReplyArea");
                      if (gridReplyArea != null)
                      {
                          gridReplyArea.Width = width *0.7;
                          gridReplyArea.Margin = new Thickness(CalculateScaledValue(width, -30, 70),0,0, 0);
                      }
                      
                      
                      var ProfileImageReplyBorder = FindDescendantByName<Border>(lbi, "ProfileImageReplyBorder");
                      if (ProfileImageReplyBorder != null)
                      {
                          ProfileImageReplyBorder.Width = avatarSize;
                          ProfileImageReplyBorder.Height = avatarSize;
                          ProfileImageReplyBorder.CornerRadius = new CornerRadius(avatarSize / 2);
                          ProfileImageReplyBorder.Margin = new Thickness(0, width < 600 ? 10 : 15, 0, 0);
                      }
                      
                      var CommentReplySendBtn = FindDescendantByName<Button>(lbi, "CommentReplySendBtn");
                      if (CommentReplySendBtn != null)
                      {
                          //CommentReplySendBtn.Width = CalculateScaledValue(width,14,30);
                          CommentReplySendBtn.FontSize = actionButtonSize;
                      }

                      
                      var gridEditArea = FindDescendantByName<Grid>(lbi, "GridEditArea");
                      if (gridEditArea != null)
                      {
                          gridEditArea.Width = width *0.8;
                      }
                });
                
                // Yanıt yorumlarına da stil uygula
                var currentMainComment = comments[i];
                if (currentMainComment.ReplyComments != null && currentMainComment.ReplyComments.Count > 0)
                {
                    var replyListBox = FindDescendantByName<ListBox>(lbi, "ReplyCommentsDisplay");
                    if (replyListBox != null)
                    {
                        ApplyResponsiveReplyStyles(width, replyListBox, currentMainComment.ReplyComments);
                    }
                }
            }
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error in ApplyResponsiveCommentStyles: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    // Yanıt yorumlarına responsive stil uygulama
    private async void ApplyResponsiveReplyStyles(double width, ListBox replyListBox, ObservableCollection<Comment> replyComments)
    {
        try
        {
            // Responsive boyutları hesapla
            var avatarSize = CalculateProfileSize(width);
            var nameFontSize = CalculateTextSize(width, 12, 18);
            var dateFontSize = CalculateTextSize(width, 10, 14);
            var replyTextSize = CalculateTextSize(width, 12, 18);
            var actionButtonSize = CalculateTextSize(width, 12, 18);
            var iconSize = Math.Max(14, Math.Min(20, (int)(width / 70)));
            
            for (int j = 0; j < replyComments.Count; j++)
            {
                var replyLbi = replyListBox.ItemContainerGenerator.ContainerFromIndex(j) as ListBoxItem;
                if (replyLbi == null) continue;
                
                await Dispatcher.UIThread.InvokeAsync(() => {
                    // Avatar boyutu
                    var replyAvatar = FindDescendantByName<Border>(replyLbi, "GravatarBorder") as Border;
                    if (replyAvatar != null)
                    {
                        replyAvatar.Width = avatarSize;
                        replyAvatar.Height = avatarSize;
                        replyAvatar.CornerRadius = new CornerRadius(avatarSize / 2);
                        replyAvatar.Margin = new Thickness(0, width < 600 ? 8 : 15, 0, 0);
                    }
                    
                    // Yanıt metni için boyut ayarlama
                    var replyTextBlock = FindDescendantByName<TextBlock>(replyLbi, "TextBlockTextReplyComment");
                    if (replyTextBlock != null)
                    {
                        double maxWidth = 0;
                        if (width >= 1016 && width <= 1063)
                        {
                            maxWidth = width * 0.63;
                        }
                        else if (width >= 669 && width <= 740)
                        {
                            maxWidth = width * 0.5;
                        }
                        else if (width >= 633 && width <= 668)
                        {
                            maxWidth = width * 0.54;
                        }
                        else if (width >= 600 && width < 633)
                        {
                            maxWidth = width * 0.5;
                        }
                        else if (width <= 430)
                        {
                            maxWidth = width * 0.47;
                        }
                        else
                        {
                            maxWidth = width * 0.6;
                        }
                        replyTextBlock.FontSize = replyTextSize;
                        replyTextBlock.MaxWidth = maxWidth;
                        // Küçük ekranlarda daha dar alan
                        var stackPanelTextReply = FindDescendantByName<StackPanel>(replyLbi, "StackPanelTExtReply");
                        if (stackPanelTextReply != null)
                        {
                            stackPanelTextReply.Width = maxWidth;
                        }
                    }
                    
                    // Kullanıcı adı ve tarih
                    var replyUserNameTextBlock = FindNameInChildren<TextBlock>(replyLbi, t => t.Foreground?.ToString() == "#FAFBFD");
                    if (replyUserNameTextBlock != null)
                    {
                        replyUserNameTextBlock.FontSize = nameFontSize;
                    }
                    
                    var replyDateTextBlock = FindNameInChildren<TextBlock>(replyLbi, t => t.Foreground?.ToString() == "#919297");
                    if (replyDateTextBlock != null)
                    {
                        replyDateTextBlock.FontSize = dateFontSize;
                        replyDateTextBlock.Margin = new Thickness(CalculateScaledValue(width, 4, 8), 
                                                      CalculateScaledValue(width, 1, 3), 0, 0);
                    }
                    
                    // İkonlar
                    var replyThumbsUpIcon = FindDescendantByName<MaterialIcon>(replyLbi, "ThumbsUpReplyComments");
                    var replyThumbsDownIcon = FindDescendantByName<MaterialIcon>(replyLbi, "ThumbsDownReplyComments");
                    var replyMenuIcon = FindDescendantByName<MaterialIcon>(replyLbi, "MenuIconReplyComment");
                    
                    if (replyThumbsUpIcon != null)
                    {
                        replyThumbsUpIcon.Width = iconSize;
                        replyThumbsUpIcon.Height = iconSize;
                    }
                    
                    if (replyThumbsDownIcon != null)
                    {
                        replyThumbsDownIcon.Width = iconSize;
                        replyThumbsDownIcon.Height = iconSize;
                    }
                    
                    if (replyMenuIcon != null)
                    {
                        replyMenuIcon.Width = iconSize;
                        replyMenuIcon.Height = iconSize;
                    }
                    
                    // Yanıt butonu
                    var replyButton2 = FindDescendantByName<TextBlock>(replyLbi, "ReplyBUtton2");
                    if (replyButton2 != null)
                    {
                        replyButton2.FontSize = actionButtonSize;
                    }
                    
                    // Yanıt için edit ve cevap alanları
                    var gridReply2Area = FindDescendantByName<Grid>(replyLbi, "GridReply2Area");
                    if (gridReply2Area != null)
                    {
                        gridReply2Area.Width = width < 600 ? width - 120 : width - 350;
                    }
                    
                    var gridEditReplyArea = FindDescendantByName<Grid>(replyLbi, "GridEditReplyArea");
                    if (gridEditReplyArea != null)
                    {
                        gridEditReplyArea.Width = width < 600 ? width - 120 : width - 350;
                    }
                });
            }
        }
        catch (System.Exception exception)
        {
            var errorMessage = $"Error in ApplyResponsiveReplyStyles: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    // Belirli kritere göre eleman arama yardımcı metodu
    private T FindNameInChildren<T>(Visual parent, Func<T, bool> criteria) where T : Visual
    {
        try
        {
            foreach (var child in parent.GetVisualChildren())
            {
                if (child is T typedChild && criteria(typedChild))
                {
                    return typedChild;
                }
                
                var result = FindNameInChildren<T>(child as Visual, criteria);
                if (result != null)
                {
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in FindNameInChildren: {ex.Message}");
        }
        
        return null;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        MainView.Instance.SizeChanged += InstanceOnSizeChanged;
    }

    public void Dispose()
    {
        FirestoreManager.MovieComments.Clear();
        FirestoreManager.TvShowComments.Clear();
        
        try
        {
            //FirestoreManager.lis.StopAsync();
            timer.Stop();
            timer.Tick -= DispatcherTimerForPlayerControlOnTick;
            timer = null;
            
            // Clear any references to UI elements or data
            if (CommentsDisplay != null)
                CommentsDisplay.ItemsSource = null;

            // Clear any other references that might cause memory leaks
            ProfileImage = null;
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
        catch (System.Exception exception)
        {
            Log.Error(exception.Message);
        }
    }
}
public class OnItemLoadingFinishedEventArgs : EventArgs
{
    public ObservableCollection<Comment> comments { get; set; }

    public OnItemLoadingFinishedEventArgs(ObservableCollection<Comment> comments)
    {
        this.comments = comments;
    }
}
