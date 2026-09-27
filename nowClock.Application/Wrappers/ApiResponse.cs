using System;
using System.Collections.Generic;
using System.Text;

namespace nowClock.Application.Wrappers
{
    public class ApiResponse<T>
    {
        public bool Succeeded { get; set; }
        public string Message { get; set;  } = string.Empty;
        public T? Data { get; set; }
        public List<String>? Errors { get; set; }

        // Constructor para respuestas exitosas
        public ApiResponse(T data, string message = "Operacion Exitosa")
        {
            Succeeded = true;
            Message = message;
            Data = data;
        }

        // Constructor para errores con mensaje
        public ApiResponse(string message)
        {
            Succeeded = false;
            Message = message;
        }

        // Constructor para errores de validacion multiples
        public ApiResponse(string message, List<string> errors)
        {
            Succeeded = false;
            Message = message;
            Errors = errors;
        }
    }


}
