In the example above, Policy Authorization was handled in two specific places: the registration in Program.cs and the attribute on the Controller.
Here is exactly where those pieces fit into the puzzle:
1. The Definition (In Program.cs)
Policy authorization starts by defining a named "Policy" that groups one or more requirements together. In my previous response, this was done here:
csharp
builder.Services.AddAuthorization(options =>
{
    // "HighClearance" is the Policy Name
    options.AddPolicy("HighClearance", policy => 
        policy.AddRequirements(new MinimumLevelRequirement(5)));
});
Use code with caution.

2. The Enforcement (In the Controller)
Once a policy is defined, you apply it using the [Authorize] attribute. This tells ASP.NET Core: "Before you even let the user into this method, check if they meet the 'HighClearance' policy."
csharp
[Authorize(Policy = "HighClearance")] // <--- Policy Authorization happens here
[HttpGet("{id}")]
public async Task<IActionResult> GetDocument(int id)
{
    // ... logic for Resource Authorization follows inside ...
}
Use code with caution.

How they work together (The Flow)
It helps to think of it as a three-gate security system:
Gate 1: Authentication (The Badge)
Code: [Authorize]
Check: Does the user have a valid JWT? (Is the signature correct? Is it expired?)
Gate 2: Policy Authorization (The Clearance)
Code: [Authorize(Policy = "HighClearance")]
Check: Does the user's JWT contain the specific claims required by the "HighClearance" policy? (e.g., Is their Level claim 
?)
Gate 3: Resource Authorization (The Key)
Code: _authService.AuthorizeAsync(User, document, ...)
Check: Now that I've pulled the data from the database, does this specific user have permission to touch this specific row? (e.g., Does document.OwnerId == User.Id?)
Summary Table
Type	Handled By	When?
Authentication	JWT Middleware	Before reaching the Controller.
Policy Auth	Authorization Middleware	Just before entering the Action method.
Resource Auth	IAuthorizationService	Inside the Action, after fetching data.
Would you like to see how to simplify the Policy setup if you only need to check for a simple Claim (like Role: Admin) without writing a custom Handler?



