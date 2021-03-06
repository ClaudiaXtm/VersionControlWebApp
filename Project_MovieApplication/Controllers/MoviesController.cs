﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_MovieApplication.Data;
using Project_MovieApplication.Models;


namespace Project_MovieApplication.Controllers
{
    [Authorize]
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MoviesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movie.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Movie movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            MovieDetailsViewModel viewModel = await GetMovieDetailsViewModelFromMovie(movie);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details([Bind("MovieId, Content, Rating, ReviewDate, UserName, UserId")] MovieDetailsViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                Review review = new Review();

                var currentUserId = _userManager.GetUserId(HttpContext.User);
                review.UserId = currentUserId;
                review.UserName = HttpContext.User.Identity.Name;
                review.Content = viewModel.Content;
                review.Rating = viewModel.Rating;
                review.ReviewDate = DateTime.Now;

                Movie movie = await _context.Movie
                    .FirstOrDefaultAsync(m => m.Id == viewModel.MovieId);

                if (movie == null)
                {
                    return NotFound();
                }

                review.MovieReview = movie;
                _context.Review.Add(review);

                CountReviews(movie);
                CalculateMovieScore(review, movie);
               
                await _context.SaveChangesAsync();

                viewModel = await GetMovieDetailsViewModelFromMovie(movie);

            }
            return View(viewModel);
        }

        private void CountReviews(Movie movie)
        {
            movie.NoOfReviews += 1;
        }

        private void CalculateMovieScore(Review review, Movie movie)
        {
            movie.AverageRating += review.Rating;
        }

        private void CalculateAverageScore(Movie movie)
        {
            movie.AverageRating = movie.AverageRating / movie.NoOfReviews;
        }

        private async Task<MovieDetailsViewModel> GetMovieDetailsViewModelFromMovie(Movie movie)
        {

            MovieDetailsViewModel viewModel = new MovieDetailsViewModel();
            viewModel.Movie = movie;

            List<Review> reviews = await GetReviewList(movie);

            viewModel.Reviews = reviews;
            return viewModel;
        }

        private async Task<List<Review>> GetReviewList(Movie movie)
        {
            return await _context.Review
                .Where(m => m.MovieReview == movie).ToListAsync();
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.

        [Authorize(Roles = "Member")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,AverageRating,MovieGenre,NoOfReviews,UserName,UserId")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                var currentUserId = _userManager.GetUserId(HttpContext.User);
                movie.UserId = currentUserId;
                movie.UserName = HttpContext.User.Identity.Name;
                movie.AverageRating = 0;
                movie.NoOfReviews = 0;
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        //  [ValidateAntiForgeryToken]

        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.

        [Authorize(Roles = "Member")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,AverageRating,MovieGenre,NoOfReviews,UserName,UserId")] Movie movie)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Delete/5

        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5

        [Authorize(Roles = "Member")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
           
            if(movie.NoOfReviews != 0)
            {
                string reviewsErr = string.Format("This movie can't be deleted because it has {0} reviews", movie.NoOfReviews);
                TempData["message"] = reviewsErr;
                return View(movie);
            }


            _context.Movie.Remove(movie);

            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }


        public void DecreaseNumberOfReviews(int Id)
        {
            Movie model = new Movie();
            if (Id != 0)
            {

                model = _context.Movie.Find(Id);
                if (model.NoOfReviews != 0)
                {
                    model.NoOfReviews = model.NoOfReviews - 1;

                    _context.SaveChanges();
                }
            }
        }

    }

   
}
