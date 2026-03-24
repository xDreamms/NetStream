using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using NetStream.Views;

namespace NetStream.Navigation
{
    public class NavigationService
    {
        private static NavigationService _instance;
        private Stack<UserControl> _navigationStack = new Stack<UserControl>();
        private ContentControl _contentControl;
        private const int MaxStackDepth = 10;

        public static NavigationService Instance => _instance ??= new NavigationService();

        // Get the current content being displayed
        public object CurrentContent => _contentControl?.Content;

        // Initialize the navigation service with a content control
        public void Initialize(ContentControl contentControl)
        {
            _contentControl = contentControl;
        }

        // Dispose a page if it implements IDisposable, and clear static Instance references
        private void DisposePage(object page)
        {
            try
            {
                if (page is ExploreMorePage exploreMorePage)
                {
                    exploreMorePage.Dispose();
                    ExploreMorePage.Instance = null;
                }
                else if (page is Home home)
                {
                    home.Dispose();
                    Home.Instance = null;
                }
                else if (page is MoviesPage moviesPage)
                {
                    moviesPage.Dispose();
                    MoviesPage.Instance = null;
                }
                else if (page is TvShowsPage tvShowsPage)
                {
                    tvShowsPage.Dispose();
                    TvShowsPage.Instance = null;
                }
                else if (page is MovieDetailsPage movieDetailsPage)
                {
                    movieDetailsPage.Dispose();
                    MovieDetailsPage.Instance = null;
                }
                else if (page is AccountPage accountPage)
                {
                    accountPage.Dispose();
                }
                else if (page is CastPageControl castPageControl)
                {
                    castPageControl.Dispose();
                }
                else if (page is PlayerWindow playerWindow)
                {
                    playerWindow.Dispose();
                }
                else if (page is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NavigationService: Error disposing page {page?.GetType().Name}: {ex.Message}");
            }
        }

        // Navigate to a new page
        public void Navigate(UserControl page)
        {
            if (_contentControl == null)
                throw new InvalidOperationException("Navigation service not initialized. Call Initialize first.");

            // Add current page to history before navigating
            if (_contentControl.Content is UserControl currentPage)
            {
                // If we're navigating away from the same type of page, dispose it instead of keeping it
                if (currentPage.GetType() == page.GetType() && currentPage != page)
                {
                    DisposePage(currentPage);
                }
                else
                {
                    _navigationStack.Push(currentPage);
                }
            }

            // Trim stack to prevent unbounded growth
            while (_navigationStack.Count > MaxStackDepth)
            {
                // Remove and dispose the oldest entries
                var excess = new Stack<UserControl>();
                while (_navigationStack.Count > MaxStackDepth)
                {
                    var oldest = _navigationStack.Pop();
                    if (_navigationStack.Count >= MaxStackDepth)
                    {
                        DisposePage(oldest);
                    }
                    else
                    {
                        excess.Push(oldest);
                    }
                }
                while (excess.Count > 0)
                {
                    _navigationStack.Push(excess.Pop());
                }
                break;
            }

            // Navigate to the new page
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _contentControl.Content = page;
            });
        }

        // Go back to the previous page
        public bool GoBack()
        {
            if (_contentControl == null)
                throw new InvalidOperationException("Navigation service not initialized. Call Initialize first.");

            if (_navigationStack.Count > 0)
            {
                // Dispose the current page before going back
                if (CurrentContent != null)
                {
                    DisposePage(CurrentContent);
                }
             
                var previousPage = _navigationStack.Pop();
                
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _contentControl.Content = previousPage;
                });
                
                return true;
            }
            
            return false;
        }

        // Check if can go back
        public bool CanGoBack => _navigationStack.Count > 0;
        
        // Clear navigation history
        public void ClearHistory()
        {
            // Dispose all pages in the stack before clearing
            while (_navigationStack.Count > 0)
            {
                var page = _navigationStack.Pop();
                DisposePage(page);
            }
            _navigationStack.Clear();
        }
    }
} 