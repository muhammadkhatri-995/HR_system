using HR_system.Interfaces;
using HR_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_system.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class RoleController : BaseController
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IAuditService _auditService;

        public RoleController(
            IRoleRepository roleRepository,
            IAuditService auditService)
        {
            _roleRepository = roleRepository;
            _auditService = auditService;
        }

        // GET: /Role
        public async Task<IActionResult> Index()
        {
            var roles = await _roleRepository.GetAllRolesAsync();

            return View(roles);
        }

        // GET: /Role/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Role/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (ModelState.IsValid)
            {
                await _roleRepository.AddAsync(role);

                // Audit Log
                await _auditService.LogAsync(
                    "Role",
                    "Create",
                    $"Role '{role.Name}' created successfully."
                );

                NotifySuccess("Role created successfully.");

                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        // GET: /Role/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _roleRepository.GetRoleByIdAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // POST: /Role/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role role)
        {
            // Safety check
            if (id != role.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(role);
            }

            await _roleRepository.updateAsync(role);

            // Audit Log
            await _auditService.LogAsync(
                "Role",
                "Update",
                $"Role '{role.Name}' (ID: {role.Id}) updated successfully."
            );

            NotifySuccess("Role updated successfully.");

            return RedirectToAction(nameof(Index));
        }

        // GET: /Role/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roleRepository.GetRoleByIdAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // POST: /Role/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Get role first so we can record its name in the audit log
            var role = await _roleRepository.GetRoleByIdAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            await _roleRepository.DeleteAsync(id);

            // Audit Log
            await _auditService.LogAsync(
                "Role",
                "Delete",
                $"Role '{role.Name}' (ID: {role.Id}) deleted successfully."
            );

            NotifySuccess("Role deleted successfully.");

            return RedirectToAction(nameof(Index));
        }
    }
}