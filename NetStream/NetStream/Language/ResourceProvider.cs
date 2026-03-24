using Avalonia.Controls;
using System;
using Avalonia;
using Avalonia.Styling;

public static class ResourceProvider
{
    // Uygulama kaynaklarından string değerini al
    public static string GetString(string key)
    {
        try
        {
            // Avalonia API'si ile uyumlu şekilde resource lookup
            if (Application.Current != null)
            {
                // First check main resources
                object resource;
                if (Application.Current.Resources.TryGetResource(key, null, out resource))
                {
                    if (resource is string value)
                    {
                        return value;
                    }
                }
                
                // Then manually check all merged dictionaries
                foreach (var dict in Application.Current.Resources.MergedDictionaries)
                {
                    object dictResource;
                    if (dict.TryGetResource(key, null, out dictResource))
                    {
                        if (dictResource is string dictValue)
                        {
                            return dictValue;
                        }
                    }
                }
                
                Console.WriteLine($"Resource key not found: {key}");
                return "not found"; // Kaynak bulunamazsa "not found" döndür
            }
            return "not found";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dil kaynağı alınamadı: {key}, Error: {ex.Message}");
            return key;
        }
    }
} 