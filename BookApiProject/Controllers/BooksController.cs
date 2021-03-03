﻿using BookApiProject.Dtos;
using BookApiProject.Models;
using BookApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookApiProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : Controller
    {
        private IBookRepository _bookRepository;
        private IAuthorRepository _authorRepository;
        private ICategoryRepository _categoryRepository;
        private IReviewRepository _reviewRepository;

        public BooksController(IBookRepository bookRepository, IAuthorRepository authorRepository, 
            ICategoryRepository categoryRepository, IReviewRepository reviewRepository)
        {
            _bookRepository = bookRepository;
            _authorRepository = authorRepository;
            _categoryRepository = categoryRepository;
            _reviewRepository = reviewRepository;
        }

        //api/books
        [HttpGet]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(200, Type = typeof(IEnumerable<BookDto>))]
        public IActionResult GetBooks()
        {
            var books = _bookRepository.GetBooks();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var booksDto = new List<BookDto>();
            foreach (var book in books)
            {
                booksDto.Add(new BookDto
                {
                    Id = book.Id,
                    Title = book.Title,
                    Isbn = book.Isbn,
                    DatePublished = book.DatePublished
                });
            }

            return Ok(booksDto);
        }

        //api/books/{bookId}
        [HttpGet("{bookId}", Name = "GetBook")]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(200, Type = typeof(BookDto))]
        public IActionResult GetBook(int bookId)
        {
            if (!_bookRepository.BookExists(bookId))
                return NotFound();

            var book = _bookRepository.GetBook(bookId);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var bookDto = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Isbn = book.Isbn,
                DatePublished = book.DatePublished
            };

            return Ok(bookDto);
        }

        //api/books/isbn/{bookIsbn}
        [HttpGet("isbn/{bookIsbn}")]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(200, Type = typeof(BookDto))]
        public IActionResult GetBook(string bookIsbn)
        {
            if (!_bookRepository.BookExists(bookIsbn))
                return NotFound();

            var book = _bookRepository.GetBook(bookIsbn);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var bookDto = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Isbn = book.Isbn,
                DatePublished = book.DatePublished
            };

            return Ok(bookDto);
        }

        //api/books/{bookId}/rating
        [HttpGet("{bookId}/rating")]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(200, Type = typeof(decimal))]
        public IActionResult GetBookRating(int bookId)
        {
            if (!_bookRepository.BookExists(bookId))
                return NotFound();

            var rating = _bookRepository.GetBookRating(bookId);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(rating);
        }

        //api/books?authId=1&authId=2&catId=1&catId=2
        [HttpPost]
        [ProducesResponseType(201, Type = typeof(Book))]
        [ProducesResponseType(400)]        
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public IActionResult CreateBook([FromQuery] List<int> authId, [FromQuery] List<int> catId, 
                                        [FromBody] Book bookToCreate)
        {
            var statusCode = ValidateBook(authId, catId, bookToCreate);

            if (!ModelState.IsValid)
                return StatusCode(statusCode.StatusCode);

            if (!_bookRepository.CreateBook(authId, catId, bookToCreate))
            {
                ModelState.AddModelError("", $"Something went wrong saving the book " +
                    $"{bookToCreate.Title}");
                return StatusCode(500, ModelState);
            }

            return CreatedAtRoute("GetBook", new { bookId = bookToCreate.Id }, bookToCreate);
        }

        //api/books/{bookId}?authId=1&authId=2&catId=1&catId=2
        [HttpPut("{bookId}")]
        [ProducesResponseType(204)] // NoContent
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public IActionResult UpdateBook(int bookId, [FromQuery] List<int> authId, [FromQuery] List<int> catId,
                                        [FromBody] Book bookToUpdate)
        {
            var statusCode = ValidateBook(authId, catId, bookToUpdate);

            if (bookId != bookToUpdate.Id)
                return BadRequest(ModelState);

            if (!_bookRepository.BookExists(bookId))
                return NotFound();

            if (!ModelState.IsValid)
                return StatusCode(statusCode.StatusCode);

            if (!_bookRepository.UpdateBook(authId, catId, bookToUpdate))
            {
                ModelState.AddModelError("", $"Something went wrong updating the book " +
                    $"{bookToUpdate.Title}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        //api/reviewers/{bookId}
        [HttpDelete("{bookId}")]
        [ProducesResponseType(204)] // NoContent
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public IActionResult DeleteBook(int bookId)
        {
            if (!_bookRepository.BookExists(bookId))
                return NotFound();

            var bookToDelete = _bookRepository.GetBook(bookId);
            var reviewsToDelete = _reviewRepository.GetReviewsOfABook(bookId);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_bookRepository.DeleteBook(bookToDelete))
            {
                ModelState.AddModelError("", $"Something went wrong deleting book {bookToDelete.Title}");
                return StatusCode(500, ModelState);
            }

            if (!_reviewRepository.DeleteReviews(reviewsToDelete.ToList()))
            {
                ModelState.AddModelError("", $"Something went wrong deleting reviews");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        private StatusCodeResult ValidateBook(List<int> authorsId, List<int> categoriesId, Book book)
        {
            if (book == null || authorsId.Count() <= 0 || categoriesId.Count() <= 0)
            {
                ModelState.AddModelError("", "Missing book, author or category");
                return BadRequest();
            }

            if (_bookRepository.IsDuplicateIsbn(book.Id, book.Isbn))
            {
                ModelState.AddModelError("", "Duplicate ISBN");
                return StatusCode(422);
            }

            foreach (var authorId in authorsId)
            {
                if (!_authorRepository.AuthorExists(authorId))
                {
                    ModelState.AddModelError("", "Author Not Found");
                    return StatusCode(404);
                }
            }

            foreach (var categoryId in categoriesId)
            {
                if (!_categoryRepository.CategoryExists(categoryId))
                {
                    ModelState.AddModelError("", "Category Not Found");
                    return StatusCode(404);
                }
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Critical Error");
                return BadRequest();
            }

            return NoContent();
        }
    }
}
