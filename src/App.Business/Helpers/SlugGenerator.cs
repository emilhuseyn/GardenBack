using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace App.Business.Helpers
{
    public static class SlugCreator
    {
        public static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var slug = input.ToLower();

            slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");

            slug = slug.Trim('-');

            return slug;
        }
    }
}
