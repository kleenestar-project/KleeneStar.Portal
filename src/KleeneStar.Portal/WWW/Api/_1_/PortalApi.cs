using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebParameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebExpress.WebCore.WebMessage;

namespace KleeneStar.Portal.WWW.Api._1_
{
    /// <summary>
    /// Shared plumbing for the portal REST endpoints (<c>/api/1/…</c> of the portal
    /// application): the JSON serializer profile, the response factories, the request
    /// body reader, and the DTO projections of the portal domain types. Keeping the
    /// wire shapes here guarantees that every endpoint emits the same contract.
    /// </summary>
    internal static class PortalApi
    {
        /// <summary>
        /// Serializer profile of the portal API: camelCase properties, camelCase enum
        /// values (<c>"waitingOnRequester"</c>), null members omitted, case-insensitive
        /// reads.
        /// </summary>
        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <summary>
        /// Wraps a payload into a JSON <c>200 OK</c> response.
        /// </summary>
        /// <param name="payload">The payload to serialize.</param>
        /// <returns>The response.</returns>
        internal static IResponse Json(object payload)
        {
            return new ResponseOK
            {
                Content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions))
            }
                .AddHeaderContentType("application/json");
        }

        /// <summary>
        /// Wraps a payload into a JSON <c>201 Created</c> response.
        /// </summary>
        /// <param name="payload">The payload to serialize.</param>
        /// <returns>The response.</returns>
        internal static IResponse Created(object payload)
        {
            return new ResponseCreated
            {
                Content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions))
            }
                .AddHeaderContentType("application/json");
        }

        /// <summary>
        /// Produces the empty <c>204 No Content</c> acknowledgement used by the
        /// state-changing actions.
        /// </summary>
        /// <returns>The response.</returns>
        internal static IResponse NoContent()
        {
            return new ResponseNoContent();
        }

        /// <summary>
        /// Wraps a validation error into a JSON <c>400 Bad Request</c> response.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The response.</returns>
        internal static IResponse Error(string message)
        {
            return new ResponseBadRequest
            {
                Content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = message }, JsonOptions))
            }
                .AddHeaderContentType("application/json");
        }

        /// <summary>
        /// Wraps a state conflict (e.g. accepting a resolution that has not been
        /// proposed) into a JSON <c>422</c> response. The concept document specifies
        /// <c>409 Conflict</c>; WebExpress 2.0.0-alpha does not ship a dedicated
        /// conflict response type, so the portal falls back to
        /// <c>422 Unprocessable Entity</c> and surfaces the conflict reason in the
        /// <c>error</c> field. When <c>ResponseConflict</c> becomes available in a
        /// later WebExpress release, this method should switch to it.
        /// </summary>
        /// <param name="message">The conflict message.</param>
        /// <returns>The response.</returns>
        internal static IResponse Conflict(string message)
        {
            return new ResponseUnprocessableEntity
            {
                Content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = message }, JsonOptions))
            }
                .AddHeaderContentType("application/json");
        }

        /// <summary>
        /// Runs <paramref name="action"/> and maps a <see cref="KleeneStar.Portal.WebManager.PortalConflictException"/>
        /// to a <c>409/422</c> response (see <see cref="Conflict"/>). Any other
        /// exception is allowed to propagate so framework-level error handling
        /// continues to work.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>The response produced by <paramref name="action"/>, or a conflict response.</returns>
        internal static IResponse RunWithConflictMapping(Func<IResponse> action)
        {
            try
            {
                return action();
            }
            catch (KleeneStar.Portal.WebManager.PortalConflictException ex)
            {
                return Conflict(ex.Message);
            }
        }

        /// <summary>
        /// Produces the <c>404 Not Found</c> response.
        /// </summary>
        /// <returns>The response.</returns>
        internal static IResponse NotFound()
        {
            return new ResponseNotFound();
        }

        /// <summary>
        /// Attempts to deserialize the request body into <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="request">The incoming request.</param>
        /// <param name="payload">The deserialized payload, or <c>default</c>.</param>
        /// <returns><see langword="true"/> when the body parsed successfully.</returns>
        internal static bool TryReadBody<T>(IRequest request, out T payload)
        {
            payload = default;

            try
            {
                var content = (request as Request)?.Content;

                if (content is not { Length: > 0 })
                {
                    return false;
                }

                payload = JsonSerializer.Deserialize<T>(content, JsonOptions);

                return payload is not null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Resolves the issue key bound by the <c>{issuekey}</c> URL segment.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The issue key, or <see langword="null"/>.</returns>
        internal static string GetIssueKey(IRequest request)
        {
            return request?.GetParameter<IssueKeyParameter>()?.Value;
        }

        /// <summary>
        /// Projects a request type onto its wire shape.
        /// </summary>
        /// <param name="requestType">The request type to project.</param>
        /// <returns>The DTO.</returns>
        internal static RequestTypeDto ToDto(IRequestType requestType)
        {
            return new RequestTypeDto
            {
                Key = requestType.Key,
                Title = requestType.Title,
                Description = requestType.Description,
                IconKey = requestType.IconKey,
                Templates = [.. requestType.Templates.Select(t => new TemplateDto
                {
                    Key = t.Key,
                    Title = t.Title,
                    Description = t.Description
                })]
            };
        }

        /// <summary>
        /// Projects an issue onto its wire shape.
        /// </summary>
        /// <param name="issue">The issue to project.</param>
        /// <param name="includeTimeline">
        /// <see langword="true"/> to include the comment timeline (single-issue
        /// retrieval); list projections omit it.
        /// </param>
        /// <returns>The DTO.</returns>
        internal static IssueDto ToDto(IIssue issue, bool includeTimeline)
        {
            return new IssueDto
            {
                Key = issue.Key,
                Title = issue.Title,
                Description = issue.Description,
                RequestTypeKey = issue.RequestType?.Key,
                RequestTypeName = issue.RequestTypeName,
                Priority = issue.Priority,
                State = issue.PortalState,
                Requester = ToDto(issue.Requester),
                AssigneeLabel = issue.AssigneeLabel,
                Created = issue.Created,
                Updated = issue.Updated,
                SharedWith = [.. issue.SharedWith.Select(ToDto)],
                Watchers = [.. issue.Watchers.Select(ToDto)],
                Comments = includeTimeline
                    ? [.. issue.Comments.Select(ToDto)]
                    : null
            };
        }

        /// <summary>
        /// Projects a participant onto its wire shape.
        /// </summary>
        /// <param name="participant">The participant, or <see langword="null"/>.</param>
        /// <returns>The DTO, or <see langword="null"/>.</returns>
        internal static ParticipantDto ToDto(IssueParticipant participant)
        {
            if (participant is null)
            {
                return null;
            }

            return new ParticipantDto
            {
                Id = participant.Id,
                Name = participant.Name,
                Email = participant.Email,
                Initials = participant.Initials
            };
        }

        /// <summary>
        /// Projects a timeline entry onto its wire shape.
        /// </summary>
        /// <param name="comment">The timeline entry.</param>
        /// <returns>The DTO.</returns>
        internal static CommentDto ToDto(IssueComment comment)
        {
            return new CommentDto
            {
                Id = comment.Id,
                Author = ToDto(comment.Author),
                IsSystem = comment.IsSystem,
                Role = comment.Role,
                Visibility = comment.Visibility,
                Timestamp = comment.Timestamp,
                Text = comment.Text
            };
        }

        /// <summary>
        /// The wire shape of a request type.
        /// </summary>
        internal sealed class RequestTypeDto
        {
            /// <summary>Gets or sets the stable request-type key.</summary>
            public string Key { get; init; }

            /// <summary>Gets or sets the display title.</summary>
            public string Title { get; init; }

            /// <summary>Gets or sets the short description.</summary>
            public string Description { get; init; }

            /// <summary>Gets or sets the icon key.</summary>
            public string IconKey { get; init; }

            /// <summary>Gets or sets the templates of the request type.</summary>
            public IReadOnlyList<TemplateDto> Templates { get; init; }
        }

        /// <summary>
        /// The wire shape of a template.
        /// </summary>
        internal sealed class TemplateDto
        {
            /// <summary>Gets or sets the stable template key.</summary>
            public string Key { get; init; }

            /// <summary>Gets or sets the display title.</summary>
            public string Title { get; init; }

            /// <summary>Gets or sets the short description.</summary>
            public string Description { get; init; }
        }

        /// <summary>
        /// The wire shape of a participant.
        /// </summary>
        internal sealed class ParticipantDto
        {
            /// <summary>Gets or sets the identity id.</summary>
            public string Id { get; init; }

            /// <summary>Gets or sets the display name.</summary>
            public string Name { get; init; }

            /// <summary>Gets or sets the e-mail address.</summary>
            public string Email { get; init; }

            /// <summary>Gets or sets the avatar monogram.</summary>
            public string Initials { get; init; }
        }

        /// <summary>
        /// The wire shape of a timeline entry.
        /// </summary>
        internal sealed class CommentDto
        {
            /// <summary>Gets or sets the comment id.</summary>
            public string Id { get; init; }

            /// <summary>Gets or sets the author, or <see langword="null"/> for system entries.</summary>
            public ParticipantDto Author { get; init; }

            /// <summary>Gets or sets whether the entry is machine narration.</summary>
            public bool IsSystem { get; init; }

            /// <summary>Gets or sets the role label of the author.</summary>
            public string Role { get; init; }

            /// <summary>Gets or sets the visibility flag.</summary>
            public string Visibility { get; init; }

            /// <summary>Gets or sets the creation timestamp.</summary>
            public DateTime Timestamp { get; init; }

            /// <summary>Gets or sets the message body.</summary>
            public string Text { get; init; }
        }

        /// <summary>
        /// The wire shape of an issue.
        /// </summary>
        internal sealed class IssueDto
        {
            /// <summary>Gets or sets the issue key.</summary>
            public string Key { get; init; }

            /// <summary>Gets or sets the title.</summary>
            public string Title { get; init; }

            /// <summary>Gets or sets the long description.</summary>
            public string Description { get; init; }

            /// <summary>Gets or sets the key of the request type.</summary>
            public string RequestTypeKey { get; init; }

            /// <summary>Gets or sets the display name of the request type.</summary>
            public string RequestTypeName { get; init; }

            /// <summary>Gets or sets the priority display code.</summary>
            public string Priority { get; init; }

            /// <summary>Gets or sets the collapsed portal lifecycle state.</summary>
            public PortalIssueState State { get; init; }

            /// <summary>Gets or sets the requester.</summary>
            public ParticipantDto Requester { get; init; }

            /// <summary>Gets or sets the assigned service label.</summary>
            public string AssigneeLabel { get; init; }

            /// <summary>Gets or sets the creation timestamp.</summary>
            public DateTime Created { get; init; }

            /// <summary>Gets or sets the last-update timestamp.</summary>
            public DateTime Updated { get; init; }

            /// <summary>Gets or sets the identities the issue is shared with.</summary>
            public IReadOnlyList<ParticipantDto> SharedWith { get; init; }

            /// <summary>Gets or sets the watching identities.</summary>
            public IReadOnlyList<ParticipantDto> Watchers { get; init; }

            /// <summary>Gets or sets the timeline, or <see langword="null"/> in list projections.</summary>
            public IReadOnlyList<CommentDto> Comments { get; init; }
        }
    }
}
