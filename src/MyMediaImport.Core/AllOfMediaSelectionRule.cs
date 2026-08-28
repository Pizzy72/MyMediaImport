namespace MyMediaImport.Core;

public sealed class AllOfMediaSelectionRule : IMediaSelectionRule
{
    private readonly IReadOnlyList<IMediaSelectionRule> _rules;

    public AllOfMediaSelectionRule(params IMediaSelectionRule[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.Length == 0 || rules.Any(rule => rule is null))
        {
            throw new ArgumentException("At least one non-null selection rule is required.", nameof(rules));
        }

        _rules = rules;
    }

    public bool IsMatch(MediaItem mediaItem)
    {
        ArgumentNullException.ThrowIfNull(mediaItem);
        return _rules.All(rule => rule.IsMatch(mediaItem));
    }
}
