using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Moderation.Persistence.Documents;

namespace VividImagination.Modules.Moderation.Services;

public record ModerationEvaluationResult(
    double MaxRiskScore,
    double AdultScore,
    double ViolenceScore,
    double HateScore,
    string? SafetyCategory,
    string Verdict,
    bool IsUnsafe,
    bool BlurRequired,
    List<RekognitionLabelInfo> Labels);

public interface IAmazonRekognitionService
{
    Task<ModerationEvaluationResult> InspectPhotoAsync(string bucket, string storageKey, CancellationToken ct = default);
}

public class AmazonRekognitionService(
    IAmazonRekognition rekognitionClient,
    ILogger<AmazonRekognitionService> logger
    )
    : IAmazonRekognitionService
{
   

    public async Task<ModerationEvaluationResult> InspectPhotoAsync(string bucket, string storageKey, CancellationToken ct = default)
    {
        try
        {
            {
                // Zero-copy S3 vision inspection using Amazon Rekognition
                var request = new DetectModerationLabelsRequest
                {
                    Image = new Image
                    {
                        S3Object = new S3Object
                        {
                            Bucket = bucket,
                            Name = storageKey
                        }
                    },
                    MinConfidence = 50f
                };

                var response = await rekognitionClient.DetectModerationLabelsAsync(request, ct);
                return EvaluateLabels(response.ModerationLabels);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon Rekognition inspection failed or timed out for {StorageKey}. Applying fail-safe blur shield policy.", storageKey);
            // NFR-04: Fail-Safe Moderation Resilience -> defaults to TaggedUnsafe (BlurRequired = true)
            return new ModerationEvaluationResult(
                MaxRiskScore: 0.50,
                AdultScore: 0.50,
                ViolenceScore: 0.0,
                HateScore: 0.0,
                SafetyCategory: "Pending Review / Fail-Safe Shield",
                Verdict: "TaggedUnsafe",
                IsUnsafe: true,
                BlurRequired: true,
                Labels: [new RekognitionLabelInfo { Name = "FailSafeShield", Confidence = 50f }]);
        }
    }

    private static ModerationEvaluationResult EvaluateLabels(List<ModerationLabel> labels)
    {
        var labelInfos = labels.Select(l => new RekognitionLabelInfo
        {
            Name = l.Name,
            ParentName = l.ParentName,
            Confidence = l.Confidence ?? 0f
        }).ToList();

        double adultScore = 0.0;
        double violenceScore = 0.0;
        double hateScore = 0.0;
        bool hasSuggestive = false;

        foreach (var label in labels)
        {
            double confidenceNorm = ((double)(label.Confidence ?? 0f)) / 100.0;
            var category = (label.ParentName ?? label.Name ?? string.Empty).ToLowerInvariant();

            if (category.Contains("nudity") || category.Contains("adult") || category.Contains("sexual"))
            {
                adultScore = Math.Max(adultScore, confidenceNorm);
            }
            if (category.Contains("suggestive") || (label.Name?.ToLowerInvariant().Contains("suggestive") == true))
            {
                hasSuggestive = true;
                adultScore = Math.Max(adultScore, Math.Max(confidenceNorm, 0.55));
            }
            if (category.Contains("violence") || category.Contains("disturbing") || category.Contains("blood"))
            {
                violenceScore = Math.Max(violenceScore, confidenceNorm);
            }
            if (category.Contains("hate") || category.Contains("symbol"))
            {
                hateScore = Math.Max(hateScore, confidenceNorm);
            }
        }

        var maxScore = Math.Max(adultScore, Math.Max(violenceScore, hateScore));

        // Three-tier policy enforcement
        if (maxScore >= 0.85)
        {
            string primaryCategory = adultScore >= 0.85 ? "Explicit Nudity" : (violenceScore >= 0.85 ? "Graphic Violence" : "Hate Symbols");
            return new ModerationEvaluationResult(maxScore, adultScore, violenceScore, hateScore, primaryCategory, "Rejected", true, true, labelInfos);
        }

        if (maxScore >= 0.50 || hasSuggestive)
        {
            string primaryCategory = hasSuggestive ? "Suggestive" : (adultScore >= 0.50 ? "Adult Content" : "Violence");
            return new ModerationEvaluationResult(Math.Max(maxScore, 0.55), adultScore, violenceScore, hateScore, primaryCategory, "TaggedUnsafe", true, true, labelInfos);
        }

        return new ModerationEvaluationResult(maxScore, adultScore, violenceScore, hateScore, null, "Approved", false, false, labelInfos);
    }

    private static ModerationEvaluationResult SimulateInspection(string storageKey)
    {
        var lower = storageKey.ToLowerInvariant();
        if (lower.Contains("nsfw") || lower.Contains("reject") || lower.Contains("explicit"))
        {
            return new ModerationEvaluationResult(0.92, 0.92, 0.1, 0.0, "Explicit Nudity", "Rejected", true, true,
                [new RekognitionLabelInfo { Name = "Explicit Nudity", Confidence = 92f }]);
        }
        if (lower.Contains("sensitive") || lower.Contains("suggestive") || lower.Contains("blur") || lower.Contains("swimwear"))
        {
            return new ModerationEvaluationResult(0.65, 0.65, 0.0, 0.0, "Suggestive Content", "TaggedUnsafe", true, true,
                [new RekognitionLabelInfo { Name = "Suggestive Attire", Confidence = 65f }]);
        }

        return new ModerationEvaluationResult(0.05, 0.05, 0.0, 0.0, null, "Approved", false, false, []);
    }
}
