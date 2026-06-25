using Brittany_Salon_Backend.Application.DTOs.Client;

namespace Brittany_Salon_Backend.Application.Services.Interfaces
{
    public interface IClientService
    {
        /// <summary>
        /// Obtiene todos los clientes
        /// </summary>
        /// <param name="onlyActive">Si es true, solo retorna clientes activos</param>
        Task<List<ClientReadDto>> GetAllAsync(bool onlyActive = false);

        /// <summary>
        /// Obtiene un cliente por su ID
        /// </summary>
        Task<ClientReadDto?> GetByIdAsync(int id);

        /// <summary>
        /// Busca clientes por nombre (búsqueda parcial)
        /// </summary>
        /// <param name="name">Texto a buscar en el nombre</param>
        /// <param name="onlyActive">Si es true, solo retorna clientes activos</param>
        Task<List<ClientReadDto>> SearchByNameAsync(string name, bool onlyActive = false);

        /// <summary>
        /// Crea un nuevo cliente
        /// </summary>
        Task<ClientReadDto> CreateAsync(ClientCreateDto dto);

        /// <summary>
        /// Actualiza un cliente existente
        /// </summary>
        /// <param name="id">ID del cliente a actualizar</param>
        /// <param name="dto">Datos a actualizar (solo los campos proporcionados)</param>
        /// <returns>True si se actualizó, False si no existe</returns>
        Task<bool> UpdateAsync(int id, ClientUpdateDto dto);

        /// <summary>
        /// Desactiva un cliente (eliminación lógica)
        /// </summary>
        /// <param name="id">ID del cliente a desactivar</param>
        /// <returns>True si se desactivó, False si no existe</returns>
        Task<bool> DeactivateAsync(int id);

        Task<bool> UpdateBalanceAsync(int clientId, decimal pendingBalance);
    }
}