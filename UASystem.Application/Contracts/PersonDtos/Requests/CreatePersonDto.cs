using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Contracts.PersonDtos.Requests
{
    public record CreatePersonDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("Title")]
        [MaxLength(50, ErrorMessage = "Title cannot exceed 50 characters.")]
        public string? Title { get; set; } 

        [System.Text.Json.Serialization.JsonPropertyName("FirstName")]
        [Required(ErrorMessage = "FirstName is required.")]
        [MaxLength(50, ErrorMessage = "FirstName cannot exceed 50 characters.")]
        public string FirstName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("MiddleName")]
        [MaxLength(50, ErrorMessage = "MiddleName cannot exceed 50 characters.")]
        public string? MiddleName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("LastName")]
        [Required(ErrorMessage = "LastName is required.")]
        [MaxLength(50, ErrorMessage = "LastName cannot exceed 50 characters.")]
        public string LastName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("Suffix")]
        [MaxLength(50, ErrorMessage = "Suffix cannot exceed 50 characters.")]
        public string? Suffix { get; set; }
    }
}
