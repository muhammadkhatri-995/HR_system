using HR_system.Interfaces;
using HR_system.Models;
using Microsoft.AspNetCore.Mvc;

namespace HR_system.Controllers
{
    public class RoleController : Controller
    {
        private readonly IRoleRepository _roleRepository;
        public RoleController(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }
        public async Task<IActionResult> Index()
        {
            var roles = await _roleRepository.GetAllRolesAsync();
            return View(roles);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (ModelState.IsValid)
            {
                await _roleRepository.AddAsync(role);
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }
        // GET: /Role/Edit/5
        // Loads one role by Id and shows it in a pre-filled form.
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound(); // shows a 404 page if the Id doesn't exist
            }
            return View(role);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role role)
        {
            // Safety check: the Id in the URL must match the Id in the submitted form.
            if (id != role.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(role);
            }

            await _roleRepository.updateAsync(role);
            return RedirectToAction(nameof(Index));
        }
        // GET: /Role/Delete/5
        // Shows a confirmation page before actually deleting.
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return View(role);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _roleRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }


    }
}
