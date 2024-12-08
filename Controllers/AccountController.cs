using Job_Portal_Web.Models; // Ensure this points to your models namespace
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json; // Ensure you have Newtonsoft.Json NuGet package installed
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

public class AccountController : Controller
{
    private readonly HttpClient _httpClient;
    private object _jobService;

    public AccountController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
   
    [HttpGet]
    public IActionResult Register()
    {
        var model = new RegisterViewModel();
        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var jsonData = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://localhost:44318/api/company/register", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Login");
            }
            else
            {
                ModelState.AddModelError("", "Error during registration");
            }
        }

        return View(model);
    }
    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl; 
        var model = new LoginViewModel();
        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            var jsonData = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://localhost:44318/api/company/login", content);

            if (response.IsSuccessStatusCode)
            {
                var userRole = "Admin"; // This should come from your API, hardcoded here for demonstration.
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, userRole)
            };

                var identity = new ClaimsIdentity(claims, "Cookies");
                await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(identity));

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("ApplyJobList");
            }
            else
            {
                ModelState.AddModelError("", "Invalid login credentials");
            }
        }

        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return RedirectToAction("Login", "Account");
    }
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await _httpClient.GetAsync("https://localhost:44318/api/company");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var companies = JsonConvert.DeserializeObject<List<CompanyViewModel>>(content);
            return View(companies);
        }
        else
        {
            ModelState.AddModelError(string.Empty, "An error occurred while retrieving the company list.");
            return View(new List<CompanyViewModel>());
        }
    }
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CompanyViewModel model)
    {
        if (ModelState.IsValid)
        {
            var jsonData = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://localhost:44318/api/company/create", content);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Error creating company");
            }
        }

        return View(model);
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var response = await _httpClient.GetAsync($"https://localhost:44318/api/company/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        var content = await response.Content.ReadAsStringAsync();
        var company = JsonConvert.DeserializeObject<CompanyViewModel>(content);

        return View(company);
    }
    [Authorize(Roles = "Admin")]
    [HttpPost("Edit/{id}")]
    public async Task<IActionResult> Edit(int id, CompanyViewModel model)
    {
        if (ModelState.IsValid)
        {
            var jsonData = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"https://localhost:44318/api/company/update/{id}", content);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Error updating company");
            }
        }

        return View(model);
    }
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> JobsIndex()
    {
        var response = await _httpClient.GetAsync("https://localhost:44318/api/company/jobs");
        if (response.IsSuccessStatusCode)
        {
            var companiesResponse = await _httpClient.GetAsync("https://localhost:44318/api/company");
            var listCompany = new List<CompanyViewModel>();
            if (companiesResponse.IsSuccessStatusCode)
            {

                var compnies = await companiesResponse.Content.ReadAsStringAsync();
                listCompany = JsonConvert.DeserializeObject<List<CompanyViewModel>>(compnies);
            }



            var content = await response.Content.ReadAsStringAsync();
            var jobs = JsonConvert.DeserializeObject<List<JobViewModel>>(content);

            if (listCompany != null && listCompany.Count() > 0 && jobs.Count() > 0)
            {
                foreach (var item in jobs)
                {
                    var company = listCompany.Where(x => x.Id == item.CompanyId)?.ToList();
                    if (company != null && company.Count() > 0)
                    {
                        item.CompanyName = company.FirstOrDefault().CompanyName;
                    }

                }
            }

            return View(jobs);
        }
        else
        {
            ModelState.AddModelError(string.Empty, "An error occurred while retrieving the job list.");
            return View(new List<JobViewModel>());
        }
    }
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> CreateJob()
    {
        var companiesResponse = await _httpClient.GetAsync("https://localhost:44318/api/company");
        if (!companiesResponse.IsSuccessStatusCode)
        {
            return NotFound();
        }

        var companiesContent = await companiesResponse.Content.ReadAsStringAsync();
        var companies = JsonConvert.DeserializeObject<List<CompanyViewModel>>(companiesContent);
        var jobViewModel = new JobViewModel
        {
            Companies = companies
        };

        return View(jobViewModel);
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateJob(JobViewModel model)
    {
        System.Diagnostics.Debug.WriteLine($"CompanyId: {model.CompanyId}");
        if (ModelState.IsValid)
        {
            var jsonData = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://localhost:44318/api/company/jobs/create", content);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("JobsIndex");
            }
            else
            {
                ModelState.AddModelError("", "Error creating job");
            }
        }
        if (model.Companies == null || !model.Companies.Any())
        {
            var companiesResponse = await _httpClient.GetAsync("https://localhost:44318/api/company");
            if (companiesResponse.IsSuccessStatusCode)
            {
                var companiesContent = await companiesResponse.Content.ReadAsStringAsync();
                model.Companies = JsonConvert.DeserializeObject<List<CompanyViewModel>>(companiesContent);
            }
        }

        var selectedCompanyName = model.Companies?.FirstOrDefault(c => c.Id == model.CompanyId)?.CompanyName;
        System.Diagnostics.Debug.WriteLine($"Selected Company Name: {selectedCompanyName}");

        return View(model);
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("EditJob/{id}")]
    public async Task<IActionResult> EditJob(int id)
    {
        var jobResponse = await _httpClient.GetAsync($"https://localhost:44318/api/company/jobs/{id}");
        if (!jobResponse.IsSuccessStatusCode)
        {
            return NotFound();
        }

        var jobContent = await jobResponse.Content.ReadAsStringAsync();
        var job = JsonConvert.DeserializeObject<JobViewModel>(jobContent);

        var companiesResponse = await _httpClient.GetAsync("https://localhost:44318/api/company");
        var companiesContent = await companiesResponse.Content.ReadAsStringAsync();
        job.Companies = JsonConvert.DeserializeObject<List<CompanyViewModel>>(companiesContent);

        return View(job);
    }
    [Authorize(Roles = "Admin")]
    [HttpPost("EditJob/{id}")]
    public async Task<IActionResult> EditJob(int id, JobViewModel model)
    {
        if (ModelState.IsValid)
        {
            var jsonData = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"https://localhost:44318/api/company/jobs/update/{id}", content);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("JobsIndex");
            }
            else
            {
                ModelState.AddModelError("", "Error updating job");
            }
        }
        var companiesResponse = await _httpClient.GetAsync("https://localhost:44318/api/company");
        var companiesContent = await companiesResponse.Content.ReadAsStringAsync();
        model.Companies = JsonConvert.DeserializeObject<List<CompanyViewModel>>(companiesContent);

        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> ApplyJobList()
    {
        var response = await _httpClient.GetAsync("https://localhost:44318/api/company/jobs");

        if (response.IsSuccessStatusCode)
        {
            var companiesResponse = await _httpClient.GetAsync("https://localhost:44318/api/company");
            var listCompany = new List<CompanyViewModel>();

            if (companiesResponse.IsSuccessStatusCode)
            {
                var companiesContent = await companiesResponse.Content.ReadAsStringAsync();
                listCompany = JsonConvert.DeserializeObject<List<CompanyViewModel>>(companiesContent);
            }

            var content = await response.Content.ReadAsStringAsync();
            var jobs = JsonConvert.DeserializeObject<List<JobViewModel>>(content);

            if (listCompany != null && listCompany.Count() > 0 && jobs.Count() > 0)
            {
                foreach (var item in jobs)
                {
                    var company = listCompany.FirstOrDefault(x => x.Id == item.CompanyId);
                    if (company != null)
                    {
                        item.CompanyName = company.CompanyName;
                    }
                }
            }

            return View(jobs);
        }
        else
        {
            ModelState.AddModelError(string.Empty, "An error occurred while retrieving the job list.");
            return View(new List<JobViewModel>());
        }
    }
    [HttpGet("ApplyJobDetails/{id}")]
    [Authorize]
    public async Task<IActionResult> ApplyJobDetails(int id)
    {
        var jobResponse = await _httpClient.GetAsync($"https://localhost:44318/api/company/jobs/{id}");

        if (!jobResponse.IsSuccessStatusCode)
        {
            return NotFound("Job not found.");
        }
        var jobContent = await jobResponse.Content.ReadAsStringAsync();
        var job = JsonConvert.DeserializeObject<JobViewModel>(jobContent);
        return View(job);
    }



}
