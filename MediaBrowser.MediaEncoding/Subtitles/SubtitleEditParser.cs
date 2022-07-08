using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Extensions;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.Common;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using SubtitleFormat = Nikse.SubtitleEdit.Core.SubtitleFormats.SubtitleFormat;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// SubStation Alpha subtitle parser.
    /// </summary>
    public class SubtitleEditParser : ISubtitleParser
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleEditParser"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public SubtitleEditParser(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public SubtitleTrackInfo Parse(Stream stream, string fileExtension)
        {
            var subtitleFormat = SubtitleFormat.AllSubtitleFormats.FirstOrDefault(asf => asf.Extension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
            if (subtitleFormat == null)
            {
                throw new ArgumentException("Unsupported format: " + fileExtension);
            }

            var lines = stream.ReadAllLines().ToList();
            var subtitle = new Subtitle();
            subtitleFormat.LoadSubtitle(subtitle, lines, fileExtension);
            if (subtitleFormat.ErrorCount > 0)
            {
                _logger.LogError("{ErrorCount} errors encountered while parsing subtitle", subtitleFormat.ErrorCount);
            }

            var trackInfo = new SubtitleTrackInfo();
            int len = subtitle.Paragraphs.Count;
            var trackEvents = new SubtitleTrackEvent[len];
            for (int i = 0; i < len; i++)
            {
                var p = subtitle.Paragraphs[i];
                trackEvents[i] = new SubtitleTrackEvent(p.Number.ToString(CultureInfo.InvariantCulture), p.Text)
                {
                    StartPositionTicks = p.StartTime.TimeSpan.Ticks,
                    EndPositionTicks = p.EndTime.TimeSpan.Ticks
                };
            }

            trackInfo.TrackEvents = trackEvents;
            return trackInfo;
        }
    }
}
