using System.Security.Cryptography;
using System.Text;
using JobInMultiInstance.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace sample.Controllers;
/// <summary>
/// 工作任务
/// </summary>
[Route("api/[controller]"), ApiController]
public class JobController : Controller
{
    private readonly IMultiInstanceJobService _multiInstanceJobService;
    /// <summary>
    /// 
    /// </summary>
    public JobController(IMultiInstanceJobService multiInstanceJobService)
    {
        _multiInstanceJobService = multiInstanceJobService;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> Get(string key)
    {
        return Ok(await _multiInstanceJobService.GetJob<SampleJobHandler>(key));
    }
    
    [HttpPost("start/{key}")]
    public async Task<IActionResult>  Start(string key)
    {
        await _multiInstanceJobService.RunJobAsync<SampleJobHandler>(key,"test");
        return Ok();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpPost("stop/{key}")]
    public async Task<IActionResult> StopJob(string key)
    {
        await _multiInstanceJobService.KillJobAsync<SampleJobHandler>(key);
        return Ok("success");
    }
    
}