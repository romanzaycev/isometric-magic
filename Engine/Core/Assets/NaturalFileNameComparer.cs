namespace IsometricMagic.Engine.Core.Assets
{
    internal sealed class NaturalFileNameComparer : IComparer<string>
    {
        public static readonly NaturalFileNameComparer Instance = new();

        private NaturalFileNameComparer()
        {
        }

        public int Compare(string? left, string? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            var i = 0;
            var j = 0;

            while (i < left.Length && j < right.Length)
            {
                var c1 = left[i];
                var c2 = right[j];

                var isDigit1 = char.IsDigit(c1);
                var isDigit2 = char.IsDigit(c2);

                if (isDigit1 && isDigit2)
                {
                    var startI = i;
                    var startJ = j;

                    while (i < left.Length && char.IsDigit(left[i])) i++;
                    while (j < right.Length && char.IsDigit(right[j])) j++;

                    var numberLeft = left[startI..i].TrimStart('0');
                    var numberRight = right[startJ..j].TrimStart('0');

                    if (numberLeft.Length != numberRight.Length)
                    {
                        return numberLeft.Length.CompareTo(numberRight.Length);
                    }

                    var numericCompare = string.Compare(numberLeft, numberRight, StringComparison.Ordinal);
                    if (numericCompare != 0)
                    {
                        return numericCompare;
                    }

                    var tokenLengthCompare = (i - startI).CompareTo(j - startJ);
                    if (tokenLengthCompare != 0)
                    {
                        return tokenLengthCompare;
                    }

                    continue;
                }

                var charCompare = char.ToLowerInvariant(c1).CompareTo(char.ToLowerInvariant(c2));
                if (charCompare != 0)
                {
                    return charCompare;
                }

                i++;
                j++;
            }

            return left.Length.CompareTo(right.Length);
        }
    }
}
