﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NapocaBike.Data;
using NapocaBike.Models;
using Newtonsoft.Json;

namespace NapocaBike.Pages.BikeRentalLocations
{
    public class IndexModel : BasePageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger, UserManager<IdentityUser> userManager, NapocaBikeContext context, RoleManager<IdentityRole> roleManager)
            : base(userManager, context, roleManager)
        {
            _logger = logger;
        }
        public Member CurrentMember { get; set; }
        public IList<BikeRentalLocation> BikeRentalLocation { get; set; } = default!;
        

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                CurrentMember = await _context.Member.FirstOrDefaultAsync(m => m.Email == user.Email);
            }

            await FetchAndSaveData();
            if (_context.BikeRentalLocation != null)
            {
                BikeRentalLocation = await _context.BikeRentalLocation.ToListAsync();
            }
           
            await LoadUserDataAsync();
        }

        public async Task FetchAndSaveData()
        {
            // Creating the HttpClient with decompression methods.
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };

            using var client = new HttpClient(handler);
            var apiKey = "twTexM54vieO";
            var projectId = "t575N9MFXRz8";
            var url = $"https://parsehub.com/api/v2/projects/{projectId}/last_ready_run/data?api_key={apiKey}&format=json";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var parseHubResponse = JsonConvert.DeserializeObject<ParseHubResponse>(responseBody);
            var bikeRentalLocations = parseHubResponse.SelecPin;

           
            _context.BikeRentalLocation.RemoveRange(_context.BikeRentalLocation);
            await _context.SaveChangesAsync();

          
            foreach (var bikeRentalLocation in bikeRentalLocations)
            {
                int startIndex = bikeRentalLocation.Adress.IndexOf("(");
                int endIndex = bikeRentalLocation.Adress.LastIndexOf(")");

                if (startIndex != -1 && endIndex != -1)
                {
                    bikeRentalLocation.Adress = bikeRentalLocation.Adress.Remove(endIndex, 1).Remove(startIndex, 1).Trim();
                }

                _context.BikeRentalLocation.Add(bikeRentalLocation);
            }

            await _context.SaveChangesAsync();
        }

    }
}
