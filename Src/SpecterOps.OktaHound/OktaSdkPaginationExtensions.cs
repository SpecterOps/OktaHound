using System.Runtime.CompilerServices;
using System.Web;
using Okta.Sdk.Api;
using Okta.Sdk.Client;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound;

internal static class OktaSdkPaginationExtensions
{
    public static async IAsyncEnumerable<ResourceSet> ListAllResourceSets(
        this RoleCResourceSetApi resourceSetApi,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? after = null;

        do
        {
            ResourceSets resourceSets = await resourceSetApi
                .ListResourceSetsAsync(after: after, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (ResourceSet resourceSet in resourceSets._ResourceSets ?? [])
            {
                yield return resourceSet;
            }

            after = ExtractAfterCursor(resourceSets.Links?.Next?.Href);
        }
        while (after is not null);
    }

    public static async IAsyncEnumerable<IamRole> ListAllRoles(
        this RoleECustomApi roleApi,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? after = null;

        do
        {
            IamRoles roles = await roleApi
                .ListRolesAsync(after: after, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (IamRole role in roles.Roles ?? [])
            {
                yield return role;
            }

            after = ExtractAfterCursor(roles.Links?.Next?.Href);
        }
        while (after is not null);
    }

    public static async IAsyncEnumerable<RoleAssignedUser> ListAllUsersWithRoleAssignments(
        this RoleAssignmentAUserApi roleAssignmentApi,
        int? limit = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? after = null;

        do
        {
            RoleAssignedUsers privilegedUsers = await roleAssignmentApi
                .ListUsersWithRoleAssignmentsAsync(after: after, limit: limit, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (RoleAssignedUser privilegedUser in privilegedUsers.Value ?? [])
            {
                yield return privilegedUser;
            }

            after = ExtractAfterCursor(privilegedUsers.Links?.Next?.Href);
        }
        while (after is not null);
    }

    public static async IAsyncEnumerable<ResourceSetBindingRole> ListAllBindings(
        this RoleDResourceSetBindingApi resourceSetBindingApi,
        string resourceSetIdOrLabel,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? after = null;

        do
        {
            ResourceSetBindings bindings = await resourceSetBindingApi
                .ListBindingsAsync(resourceSetIdOrLabel, after: after, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (ResourceSetBindingRole binding in bindings.Roles ?? [])
            {
                yield return binding;
            }

            after = ExtractAfterCursor(bindings.Links?.Next?.Href);
        }
        while (after is not null);
    }

    public static async IAsyncEnumerable<ResourceSetBindingMember> ListAllMembersOfBinding(
        this RoleDResourceSetBindingMemberApi resourceSetBindingMemberApi,
        string resourceSetIdOrLabel,
        string roleIdOrLabel,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? after = null;

        do
        {
            ResourceSetBindingMembers members = await resourceSetBindingMemberApi
                .ListMembersOfBindingAsync(resourceSetIdOrLabel, roleIdOrLabel, after: after, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (ResourceSetBindingMember member in members.Members ?? [])
            {
                yield return member;
            }

            after = ExtractAfterCursor(members.Links?.Next?.Href);
        }
        while (after is not null);
    }

    public static async IAsyncEnumerable<ResourceSetResource> ListAllResourceSetResources(
        this RoleCResourceSetResourceApi resourceSetApi,
        string resourceSetIdOrLabel,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: Due to a bug in the Okta .NET SDK, we have to use the strongly-typed endpoint for the first page of results,
        // and then switch to the generic GET endpoint for subsequent pages.
        // This is because the SDK's pagination logic doesn't properly handle pagination for this endpoint,
        // even though it does for other endpoints with similar pagination links.
        ResourceSetResources resources = await resourceSetApi
            .ListResourceSetResourcesAsync(resourceSetIdOrLabel, cancellationToken)
            .ConfigureAwait(false);

        while (true)
        {
            // Emit all objects from the current page before resolving the next page.
            foreach (ResourceSetResource resource in resources.Resources ?? [])
            {
                yield return resource;
            }

            // Read the continuation link from the response envelope.
            string? nextHref = resources.Links?.Next?.Href;

            if (nextHref is null)
            {
                // No next link means we reached the final page.
                break;
            }

            // The generic HTTP client expects a relative path/query.
            string? pathAndQuery = ConvertHrefToRelativePathAndQuery(nextHref);

            if (pathAndQuery is null)
            {
                // Stop enumeration if we cannot parse a valid continuation URL.
                yield break;
            }

            RequestOptions requestOptions = new();
            var apiResponse = await resourceSetApi.AsynchronousClient
                .GetAsync<ResourceSetResources>(pathAndQuery, requestOptions, resourceSetApi.Configuration, cancellationToken)
                .ConfigureAwait(false);

            // Continue with the next page returned by the low-level request.
            resources = apiResponse.Data ?? new ResourceSetResources
            {
                Resources = []
            };
        }
    }

    /// <summary>
    /// Extracts the opaque <c>after</c> pagination cursor from a <c>links.next.href</c> URL.
    /// </summary>
    /// <param name="nextHref">
    /// Absolute or relative URL of the next page.
    /// Examples:
    /// <list type="bullet">
    /// <item><description><c>https://tenant.okta.com/api/v1/iam/roles?after=abc123</c></description></item>
    /// <item><description><c>/api/v1/iam/roles?after=abc123</c></description></item>
    /// </list>
    /// </param>
    /// <returns>The decoded cursor value, an empty string when explicitly present with no value, or <c>null</c> when unavailable.</returns>
    private static string? ExtractAfterCursor(string? nextHref)
    {
        if (string.IsNullOrWhiteSpace(nextHref))
        {
            return null;
        }

        if (!Uri.TryCreate(nextHref, UriKind.Absolute, out Uri? nextUri))
        {
            // Support relative links (for example: "/api/v1/...?..."), which do not parse as absolute URIs.
            // "https://localhost" is used only as a temporary placeholder base to make .NET URI parsing work.
            // No HTTP request is sent to localhost.
            if (!Uri.TryCreate("https://localhost" + nextHref, UriKind.Absolute, out nextUri))
            {
                return null;
            }
        }

        var parsedQueryString = HttpUtility.ParseQueryString(nextUri.Query);
        return parsedQueryString["after"];
    }

    /// <summary>
    /// Converts an absolute or relative URL into a relative path and query suitable for <c>IAsynchronousClient.GetAsync()</c>.
    /// </summary>
    /// <param name="href">
    /// Absolute or relative continuation URL.
    /// Examples:
    /// <list type="bullet">
    /// <item><description><c>https://tenant.okta.com/api/v1/iam/resource-sets/rs1/resources?after=abc123</c></description></item>
    /// <item><description><c>/api/v1/iam/resource-sets/rs1/resources?after=abc123</c></description></item>
    /// </list>
    /// </param>
    /// <returns>A relative path/query string or <c>null</c> if parsing fails.</returns>
    private static string? ConvertHrefToRelativePathAndQuery(string href)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out Uri? absoluteUri))
        {
            return absoluteUri.PathAndQuery;
        }

        if (Uri.TryCreate(href, UriKind.Relative, out Uri? relativeUri))
        {
            return relativeUri.OriginalString;
        }

        return null;
    }
}
