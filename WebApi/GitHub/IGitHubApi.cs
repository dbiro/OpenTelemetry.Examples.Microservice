using System.Threading.Tasks;
using RestEase;

namespace WebApi.GitHub
{
    [Header("User-Agent", "RestEase")]
    public interface IGitHubApi
    {
        // The [Get] attribute marks this method as a GET request
        // The "users" is a relative path the a base URL, which we'll provide later
        // "{userId}" is a placeholder in the URL: the value from the "userId" method parameter is used
        [Get("users/{userId}")]
        Task<GitHubUser> GetUserAsync([Path] string userId);
    }
}
