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
    /// Shared plumbing for the portal administration REST endpoints
    /// (<c>/api/1/portal/...</c>): the JSON serializer profile, the response
    /// factories, and the request body reader. The customer portal uses
    /// compact JSON arrays instead of the heavier <c>RestApiTable&lt;T&gt;</c>
    /// pipeline — the UI consumes them via plain <c>ControlTable</c>s or via
    /// ad-hoc fetch calls.
    /// </summary>
    internal static class PortalAdminApi
    {
        /// <summary>
        /// Serializer profile of the portal admin API: camelCase properties,
        /// camelCase enum values, null members omitted, case-insensitive reads.
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
        /// Produces the empty <c>204 No Content</c> acknowledgement used by the
        /// state-changing actions.
        /// </summary>
        /// <returns>The response.</returns>
        internal static IResponse NoContent()
        {
            return new ResponseNoContent();
        }

        /// <summary>
        /// Produces the <c>400 Bad Request</c> response carrying a validation
        /// message in the <c>error</c> field.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The response.</returns>
        internal static IResponse BadRequest(string message)
        {
            return new ResponseBadRequest
            {
                Content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = message }, JsonOptions))
            }
                .AddHeaderContentType("application/json");
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
        /// Produces the <c>403 Forbidden</c> response of a caller who may not administer the
        /// class structure the endpoint addresses.
        /// </summary>
        /// <returns>The response.</returns>
        internal static IResponse Forbidden()
        {
            return new ResponseForbidden();
        }

        /// <summary>
        /// Determines whether the caller may administer a class from the portal: publish it as
        /// a request type, flag its templates, change its fields - and read that structure.
        /// </summary>
        /// <remarks>
        /// The portal administration is the class administration of the operator side under
        /// another roof, so it asks the same question on the same chain (class, then workspace;
        /// administration fails closed). These endpoints used to answer anybody, an anonymous
        /// caller included.
        /// </remarks>
        /// <param name="classId">The class, or <see langword="null"/> when it did not resolve.</param>
        /// <param name="request">The request.</param>
        /// <returns><see langword="true"/> when the caller may proceed.</returns>
        internal static bool MayAdminister(Guid? classId, IRequest request)
        {
            return KleeneStar.Core.WebRestApi.ContentAuthorization.MayAdminister(classId, request);
        }

        /// <summary>
        /// Determines whether the caller may administer the class a field belongs to.
        /// </summary>
        /// <param name="fieldId">The field.</param>
        /// <param name="request">The request.</param>
        /// <returns><see langword="true"/> when the caller may proceed.</returns>
        internal static bool MayAdministerField(Guid fieldId, IRequest request)
        {
            return MayAdminister(KleeneStar.Core.CoreHub.FieldManager.GetField(fieldId)?.ClassId, request);
        }

        /// <summary>
        /// Determines whether the caller may administer the class a form belongs to.
        /// </summary>
        /// <param name="formId">The form.</param>
        /// <param name="request">The request.</param>
        /// <returns><see langword="true"/> when the caller may proceed.</returns>
        internal static bool MayAdministerForm(Guid formId, IRequest request)
        {
            var form = KleeneStar.Core.CoreHub.FormManager
                .GetForms(new WebExpress.WebIndex.Queries.Query<KleeneStar.Model.Entities.Form>())
                .FirstOrDefault(f => f.Id == formId);

            return MayAdminister(form?.ClassId, request);
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
    }
}
