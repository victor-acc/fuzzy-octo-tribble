using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using glamping_addventure3.Models;
using Microsoft.AspNetCore.Authorization;

namespace glamping_addventure3.Controllers
{
    [Authorize]
    public class AbonoesController : Controller
    {
        private readonly GlampingAddventure3Context _context;

        public AbonoesController(GlampingAddventure3Context context)
        {
            _context = context;
        }

        // GET: Abonoes
        public async Task<IActionResult> Index(int? idReserva)
        {
            if (idReserva == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reservas
                .Include(r => r.NroDocumentoClienteNavigation) // Incluir el cliente relacionado
                .FirstOrDefaultAsync(r => r.IdReserva == idReserva);

            if (reserva == null)
            {
                return NotFound();
            }

            var abonos = _context.Abonos
                .Include(a => a.IdreservaNavigation)
                .Where(a => a.Idreserva == idReserva);

            ViewData["IdReservaActual"] = idReserva;
            ViewData["NombreCliente"] = reserva.NroDocumentoClienteNavigation.Nombre; // Pasar el nombre del cliente a la vista
            return View(await abonos.ToListAsync());
        }

        public IActionResult GenerarPDF(int id)
        {
            // Obtener el abono desde la base de datos
            var abono = _context.Abonos
                .Include(a => a.IdreservaNavigation)
                .FirstOrDefault(a => a.Idabono == id);

            if (abono == null)
            {
                return NotFound();
            }

            // Crear un nuevo documento PDF
            var document = new PdfDocument();
            document.Info.Title = $"Detalle del Abono {abono.Idabono}";

            // Agregar una página al documento
            var page = document.AddPage();
            var graphics = XGraphics.FromPdfPage(page);

            // Definir la fuente con estilos correctos
            var fontRegular = new XFont("Arial", 12);
            var fontBold = new XFont("Arial", 16);

            // Escribir contenido en el PDF
            int yPosition = 40; // Coordenada Y inicial

            graphics.DrawString("Detalle del Abono", fontBold, XBrushes.Black, new XPoint(50, yPosition));
            yPosition += 40;

            graphics.DrawString($"ID del Abono: {abono.Idabono}", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
            yPosition += 20;

            graphics.DrawString($"ID de la Reserva: {abono.Idreserva}", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
            yPosition += 20;

            graphics.DrawString($"Fecha de Abono: {abono.FechaAbono:yyyy-MM-dd}", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
            yPosition += 20;

            graphics.DrawString($"Cantidad del Abono: {abono.CantAbono:C}", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
            yPosition += 20;

            graphics.DrawString($"Valor Deuda: {abono.ValorDeuda:C}", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
            yPosition += 20;

            graphics.DrawString($"Porcentaje: {abono.Porcentaje}%", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
            yPosition += 20;

            graphics.DrawString($"Pendiente: {abono.Pendiente:C}", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
            yPosition += 20;

            graphics.DrawString($"Estado: {(abono.Estado ? "Activo" : "Anulado")}", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
            yPosition += 40;

            // Verificar si existe un comprobante
            if (abono.Comprobante != null && abono.Comprobante.Length > 0)
            {
                try
                {
                    // Crear un MemoryStream desde los bytes del comprobante
                    using (var ms = new MemoryStream(abono.Comprobante))
                    {
                        // Cargar la imagen desde el MemoryStream
                        var comprobanteImage = XImage.FromStream(ms);

                        // Ajustar dimensiones y posición de la imagen en el PDF
                        double imageWidth = 200; // Ancho deseado
                        double imageHeight = comprobanteImage.PixelHeight * (imageWidth / comprobanteImage.PixelWidth); // Mantener proporción

                        // Dibujar la imagen en el PDF
                        graphics.DrawImage(comprobanteImage, 50, yPosition, imageWidth, imageHeight);
                    }
                }
                catch (Exception ex)
                {
                    graphics.DrawString($"Error al cargar el comprobante: {ex.Message}", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
                }
            }
            else
            {
                graphics.DrawString("Comprobante no disponible.", fontRegular, XBrushes.Black, new XPoint(50, yPosition));
            }

            // Guardar el PDF en memoria y devolverlo como descarga
            using (var stream = new MemoryStream())
            {
                document.Save(stream, false);
                return File(stream.ToArray(), "application/pdf", $"Abono_{abono.Idabono}.pdf");
            }
        }
        // GET: Abonoes/Details/5
        public async Task<IActionResult> Details(int? id, int? idReserva)
        {
            if (id == null)
            {
                return NotFound();
            }

            var abono = await _context.Abonos
                .Include(a => a.IdreservaNavigation)
                .FirstOrDefaultAsync(m => m.Idabono == id);

            if (abono == null)
            {
                return NotFound();
            }
            ViewData["IdReserva"] = idReserva;

            ViewData["Comprobante"] = abono.Comprobante != null
                ? $"data:image/png;base64,{Convert.ToBase64String(abono.Comprobante)}"
                : null;
            return PartialView("_DetailsPartial", abono);
        }

        // GET: Abonoes/Create
        public IActionResult Create(int? idReserva)
        {
            if (idReserva == null)
            {
                return NotFound();
            }

            var reserva = _context.Reservas.Find(idReserva);
            if (reserva == null)
            {
                return NotFound();
            }

            ViewData["IdReserva"] = idReserva;
            ViewData["MontoTotal"] = reserva.MontoTotal; // Suponiendo que la tabla Reserva tiene un campo MontoTotal.
            return View();
        }


        // POST: Abonoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Idabono,Idreserva,FechaAbono,ValorDeuda,Porcentaje,Pendiente,CantAbono,Estado")] Abono abono, IFormFile comprobante)
        {
            // Obtener la reserva asociada
            var reserva = await _context.Reservas
                .Include(r => r.Abonos)
                .FirstOrDefaultAsync(r => r.IdReserva == abono.Idreserva);

            if (reserva == null)
            {
                ModelState.AddModelError("", "La reserva no existe.");
                return View(abono);
            }

            // Calcular total de abonos previos (excluyendo anulados - Estado = 1)
            var totalAbonosPrevios = reserva.Abonos
                .Where(a => a.Estado == false) // Solo incluir abonos activos
                .Sum(a => a.CantAbono);

            // Calcular el pendiente
            abono.Pendiente = reserva.MontoTotal - totalAbonosPrevios;

            // Calcular el porcentaje del abono actual sobre el monto total de la reserva
            abono.Porcentaje = (abono.CantAbono / reserva.MontoTotal) * 100;

            // Validaciones
            if (totalAbonosPrevios == 0 && abono.Porcentaje < 50)
            {
                ModelState.AddModelError("", "El primer abono debe ser al menos el 50% del valor de la deuda.");
            }

            if ((totalAbonosPrevios + abono.CantAbono) > reserva.MontoTotal)
            {
                ModelState.AddModelError("", "No se pueden realizar más abonos, ya se ha completado el 100%.");
            }

            if (ModelState.IsValid)
            {
                // Procesar comprobante
                if (comprobante != null && comprobante.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await comprobante.CopyToAsync(memoryStream);
                        abono.Comprobante = memoryStream.ToArray(); // Convertir la imagen a binario
                    }
                }

                // Guardar abono
                _context.Add(abono);
                var totalAbonoActualizado = totalAbonosPrevios + abono.CantAbono;

                    if (totalAbonoActualizado >= reserva.MontoTotal)
                    {
                        // Cambiar a "Confirmado" si el total abonado cubre la reserva
                        reserva.IdEstadoReserva = 3; // Suponiendo que 2 es "Confirmado"
                    }
                    else if (totalAbonoActualizado > 0)
                    {
                        // Cambiar a "Por Confirmar" si hay algún abono pero no está completo
                        reserva.IdEstadoReserva = 2; // Suponiendo que 4 es "Por Confirmar"
                    }

                    // Guardar cambios en la reserva
                    _context.Update(reserva);
                await _context.SaveChangesAsync();

                // Redirigir al índice de abonos para la reserva específica
                return RedirectToAction(nameof(Index), new { idReserva = abono.Idreserva });
            }

            // Asegurarte de que los datos necesarios estén disponibles para la vista
            ViewData["IdReserva"] = abono.Idreserva;
            ViewData["MontoTotal"] = reserva.MontoTotal;

            return View(abono);
        }
        // POST: Abonoes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Idabono,Idreserva,FechaAbono,ValorDeuda,Porcentaje,Pendiente,CantAbono,Comprobante,Estado")] Abono abono)
        {
            if (id != abono.Idabono)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(abono);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (abono == null)
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
            ViewData["Idreserva"] = new SelectList(_context.Reservas, "IdReserva", "IdReserva", abono.Idreserva);
            return View(abono);
        }

        // GET: Abonoes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var abono = await _context.Abonos
                .Include(a => a.IdreservaNavigation)
                .FirstOrDefaultAsync(m => m.Idabono == id);
            if (abono == null)
            {
                return NotFound();
            }

            return View(abono);
        }

        // POST: Abonoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var abono = await _context.Abonos.FindAsync(id);
            if (abono != null)
            {
                _context.Abonos.Remove(abono);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Abonos/Anular/5
        // POST: Abonos/Anular/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int id)
        {
            var abono = await _context.Abonos.FindAsync(id);
            if (abono == null)
            {
                return NotFound();
            }

            // Marcar el abono como anulado
            abono.Estado = true;

            try
            {
                _context.Update(abono);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al anular abono: {ex.Message}");
                ModelState.AddModelError("", "Se produjo un error al anular el abono.");
            }

            return RedirectToAction("Index", new { idReserva = abono.Idabono });
        }

    }
}
