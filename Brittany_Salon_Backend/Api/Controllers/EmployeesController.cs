using Brittany_Salon_Backend.Application.DTOs.Employee;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Brittany_Salon_Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        /// <summary>
        /// Obtiene todos los empleados
        /// </summary>
        /// <param name="onlyActive">Si es true, solo retorna empleados activos</param>
        /// <returns>Lista de empleados</returns>
        /// <response code="200">Lista de empleados</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<EmployeeReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EmployeeReadDto>>> GetAll([FromQuery] bool onlyActive = false)
        {
            var employees = await _employeeService.GetAllAsync(onlyActive);
            return Ok(employees);
        }

        /// <summary>
        /// Busca empleados por nombre (busqueda parcial)
        /// </summary>
        /// <param name="name">Texto a buscar en el nombre</param>
        /// <param name="onlyActive">Si es true, solo retorna empleados activos</param>
        /// <returns>Lista de empleados que coinciden</returns>
        /// <response code="200">Lista de empleados encontrados</response>
        /// <response code="400">Parametro de busqueda invalido</response>
        [HttpGet("search/by-name")]
        [ProducesResponseType(typeof(List<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<EmployeeReadDto>>> SearchByName(
            [FromQuery] string name, 
            [FromQuery] bool onlyActive = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new ErrorResponse { Message = "El parametro 'name' es requerido." });

            var employees = await _employeeService.SearchByNameAsync(name, onlyActive);
            return Ok(employees);
        }

        /// <summary>
        /// Busca empleados por especialidad (busqueda parcial)
        /// </summary>
        /// <param name="specialty">Texto a buscar en la especialidad</param>
        /// <param name="onlyActive">Si es true, solo retorna empleados activos</param>
        /// <returns>Lista de empleados que coinciden</returns>
        /// <response code="200">Lista de empleados encontrados</response>
        /// <response code="400">Parametro de busqueda invalido</response>
        [HttpGet("search/by-specialty")]
        [ProducesResponseType(typeof(List<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<EmployeeReadDto>>> SearchBySpecialty(
            [FromQuery] string specialty, 
            [FromQuery] bool onlyActive = false)
        {
            if (string.IsNullOrWhiteSpace(specialty))
                return BadRequest(new ErrorResponse { Message = "El parametro 'specialty' es requerido." });

            var employees = await _employeeService.SearchBySpecialtyAsync(specialty, onlyActive);
            return Ok(employees);
        }

        /// <summary>
        /// Obtiene un empleado por su ID
        /// </summary>
        /// <param name="id">ID del empleado</param>
        /// <returns>Empleado encontrado</returns>
        /// <response code="200">Empleado encontrado</response>
        /// <response code="404">Empleado no encontrado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EmployeeReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeReadDto>> GetById(int id)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            
            if (employee == null)
                return NotFound(new ErrorResponse { Message = "Empleado no encontrado." });

            return Ok(employee);
        }

        /// <summary>
        /// Registra un nuevo empleado
        /// </summary>
        /// <param name="dto">Datos del empleado a registrar (multipart/form-data)</param>
        /// <returns>Empleado creado</returns>
        /// <response code="201">Empleado creado exitosamente</response>
        /// <response code="400">Errores de validacion</response>
        /// <response code="409">Email o telefono ya registrado</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(EmployeeReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EmployeeReadDto>> Create([FromForm] EmployeeCreateDto dto)
        {
            try
            {
                var created = await _employeeService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Se encontraron errores de validacion.",
                    Errors = ex.Errors
                });
            }
            catch (DuplicateResourceException ex)
            {
                return Conflict(new ErrorResponse
                {
                    Message = ex.Message,
                    Field = ex.Field
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un empleado existente
        /// </summary>
        /// <param name="id">ID del empleado a actualizar</param>
        /// <param name="dto">Datos a actualizar (solo los campos proporcionados se actualizan)</param>
        /// <returns>NoContent si se actualizo correctamente</returns>
        /// <response code="204">Empleado actualizado exitosamente</response>
        /// <response code="400">Errores de validacion</response>
        /// <response code="404">Empleado no encontrado</response>
        /// <response code="409">Email o telefono ya registrado por otro empleado</response>
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromForm] EmployeeUpdateDto dto)
        {
            try
            {
                var updated = await _employeeService.UpdateAsync(id, dto);
                
                if (!updated)
                    return NotFound(new ErrorResponse { Message = "Empleado no encontrado." });

                return NoContent();
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Se encontraron errores de validacion.",
                    Errors = ex.Errors
                });
            }
            catch (DuplicateResourceException ex)
            {
                return Conflict(new ErrorResponse
                {
                    Message = ex.Message,
                    Field = ex.Field
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Desactiva un empleado (eliminacion logica)
        /// </summary>
        /// <param name="id">ID del empleado a desactivar</param>
        /// <returns>NoContent si se desactivo correctamente</returns>
        /// <response code="204">Empleado desactivado exitosamente</response>
        /// <response code="404">Empleado no encontrado</response>
        [HttpPatch("{id:int}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _employeeService.DeactivateAsync(id);
            
            if (!result)
                return NotFound(new ErrorResponse { Message = "Empleado no encontrado." });

            return NoContent();
        }

        /// <summary>
        /// Reactiva un empleado previamente desactivado
        /// </summary>
        /// <param name="id">ID del empleado a reactivar</param>
        /// <returns>NoContent si se reactivo correctamente</returns>
        /// <response code="204">Empleado reactivado exitosamente</response>
        /// <response code="404">Empleado no encontrado</response>
        [HttpPatch("{id:int}/reactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reactivate(int id)
        {
            var result = await _employeeService.ReactivateAsync(id);
            
            if (!result)
                return NotFound(new ErrorResponse { Message = "Empleado no encontrado." });

            return NoContent();
        }

        /// <summary>
        /// Elimina permanentemente un empleado de la base de datos
        /// ADVERTENCIA: Esta accion no se puede deshacer
        /// </summary>
        /// <param name="id">ID del empleado a eliminar</param>
        /// <returns>NoContent si se elimino correctamente</returns>
        /// <response code="204">Empleado eliminado permanentemente</response>
        /// <response code="404">Empleado no encontrado</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePermanently(int id)
        {
            var result = await _employeeService.DeletePermanentlyAsync(id);
            
            if (!result)
                return NotFound(new ErrorResponse { Message = "Empleado no encontrado." });

            return NoContent();
        }
    }

    /// <summary>
    /// Respuesta estandar para errores de validacion
    /// </summary>
    public class ValidationErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }







}
