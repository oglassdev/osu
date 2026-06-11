// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class EditorDesignStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.EditorDesign";

        /// <summary>
        /// "Tools"
        /// </summary>
        public static LocalisableString Tools => new TranslatableString(getKey(@"tools"), @"Tools");

        /// <summary>
        /// "Animate"
        /// </summary>
        public static LocalisableString Animate => new TranslatableString(getKey(@"animate"), @"Animate");

        /// <summary>
        /// "Resources"
        /// </summary>
        public static LocalisableString Resources => new TranslatableString(getKey(@"resources"), @"Resources");

        /// <summary>
        /// "Placeholder"
        /// </summary>
        public static LocalisableString Placeholder => new TranslatableString(getKey(@"placeholder"), @"Placeholder");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
