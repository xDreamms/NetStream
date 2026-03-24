using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Serilog;
using TMDbLib.Objects.People;
using TMDbLib.Objects.TvShows;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.IO;

namespace NetStream
{
    /// <summary>
    /// Bu sınıf, Native AOT derlemede yansıma (reflection) ile kullanılan ve trimming sırasında kaldırılan
    /// türler için gerekli metadata bilgilerinin korunmasını sağlar.
    /// </summary>
    public static class NativeAOTWorkarounds
    {
        /// <summary>
        /// TMDbLib.Objects.People.PersonGender enum türü için metadata bilgilerini korumak amacıyla
        /// bu metot uygulama başlatıldığında çağrılmalıdır.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void KeepTMDbLibTypes()
        {
            var personGenderValues = Enum.GetValues<PersonGender>();
            var tvGroupTypeValues = Enum.GetValues<TvGroupType>();

            if (personGenderValues.Length > 0)
            {
            }
            
            if (tvGroupTypeValues.Length > 0)
            {
            }
        }
        
      
        
    }
} 