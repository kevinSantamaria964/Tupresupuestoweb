document.addEventListener("DOMContentLoaded", function () {
    cargarTransaccionesRecientes();
    actualizarDashboard();

    const filtroPeriodo = document.getElementById("filtroPeriodo");
    const filtrosPersonalizados = document.getElementById("filtrosPersonalizados");
    const desdeInput = document.getElementById("desde");
    const hastaInput = document.getElementById("hasta");

    filtroPeriodo.addEventListener("change", function () {
        if (this.value === "personalizado") {
            filtrosPersonalizados.style.display = "flex";
        } else {
            filtrosPersonalizados.style.display = "none";
            actualizarDashboard();
        }
    });

    desdeInput.addEventListener("change", actualizarDashboard);
    hastaInput.addEventListener("change", actualizarDashboard);

    if (filtroPeriodo.value === "personalizado") {
        filtrosPersonalizados.style.display = "flex";
    }

    // Gráfico de barras
    const ctx = document.getElementById('financialChart').getContext('2d');
    window.financialChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: [],
            datasets: [
                {
                    label: 'Ingresos',
                    data: [],
                    backgroundColor: '#00cc99'
                },
                {
                    label: 'Gastos',
                    data: [],
                    backgroundColor: '#ff4d4d'
                }
            ]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { position: 'top' }
            },
            scales: {
                y: { beginAtZero: true }
            }
        }
    });

    // Gráfico de proporción Ingresos vs Gastos
    const ctxProporcion = document.getElementById('proporcionChart').getContext('2d');
    window.proporcionChart = new Chart(ctxProporcion, {
        type: 'doughnut',
        data: {
            labels: ['Ingresos', 'Gastos'],
            datasets: [{
                data: [0, 0],
                backgroundColor: ['#00cc99', '#ff4d4d'],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { position: 'bottom' }
            }
        }
    });
});

function actualizarDashboard() {
    const periodo = document.getElementById("filtroPeriodo").value;
    const desde = document.getElementById("desde")?.value;
    const hasta = document.getElementById("hasta")?.value;

    if (periodo === 'personalizado') {
        if (!desde || !hasta) return;

        const desdeDate = new Date(desde);
        const hastaDate = new Date(hasta);
        if (desdeDate > hastaDate) {
            alert("La fecha 'Desde' no puede ser mayor que 'Hasta'.");
            return;
        }
    }

    fetch(`/Transacciones/ResumenFinanciero?periodo=${periodo}&desde=${desde}&hasta=${hasta}`)
        .then(response => response.json())
        .then(data => {
            animarNumero("balanceTotal", data.balance);
            animarNumero("ingresosTotales", data.ingresos);
            animarNumero("gastosTotales", data.gastos);

            financialChart.data.labels = data.labels;
            financialChart.data.datasets[0].data = data.ingresosPorPeriodo;
            financialChart.data.datasets[1].data = data.gastosPorPeriodo;
            financialChart.update();

            proporcionChart.data.datasets[0].data = [data.ingresos, data.gastos];
            proporcionChart.update();
        })
        .catch(error => {
            console.error('Error al actualizar dashboard:', error);
        });
}

function cargarTransaccionesRecientes() {
    fetch('/Transacciones/ObtenerTransaccionesRecientes')
        .then(response => response.json())
        .then(data => {
            const tbody = document.getElementById('tablaTransacciones');
            tbody.innerHTML = '';

            data.forEach(item => {
                const fecha = parseFecha(item.Fecha);
                const categoria = item.Categoria;
                const cantidad = item.Cantidad.toFixed(2);
                const claseCantidad = item.Cantidad >= 0 ? 'text-success' : 'text-danger';
                const simbolo = item.Cantidad >= 0 ? '+' : '-';

                const fila = document.createElement('tr');
                fila.innerHTML = `
                    <td>${fecha}</td>
                    <td>${categoria}</td>
                    <td class="${claseCantidad}">${simbolo}$${Math.abs(cantidad)}</td>
                    <td>${item.Tipo}</td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary" onclick="copiarAlPortapapeles('${item.Id}')">
                            Copiar ID
                        </button>
                    </td>
                `;
                tbody.appendChild(fila);
            });
        })
        .catch(error => {
            console.error('Error al cargar transacciones:', error);
        });
}

function copiarAlPortapapeles(texto) {
    navigator.clipboard.writeText(texto).then(() => {
        alert("ID copiado al portapapeles: " + texto);
    }).catch(err => {
        alert("No se pudo copiar el ID: " + err);
    });
}

function parseFecha(fechaStr) {
    if (fechaStr.includes('/Date(')) {
        const timestamp = parseInt(fechaStr.match(/\d+/)[0]);
        const fecha = new Date(timestamp);
        return isNaN(fecha) ? 'Fecha inválida' : fecha.toLocaleDateString('es-ES');
    }

    let fecha = new Date(fechaStr);
    if (isNaN(fecha)) {
        const partes = fechaStr.split(/[\/\-]/);
        if (partes.length === 3) {
            const [dia, mes, anio] = partes;
            fecha = new Date(`${anio}-${mes}-${dia}`);
        }
    }

    return isNaN(fecha) ? 'Fecha inválida' : fecha.toLocaleDateString('es-ES');
}

function enviarEliminacionManual(tipo) {
    const id = tipo === "ingreso"
        ? document.getElementById("idIngreso").value
        : document.getElementById("idGasto").value;

    if (!confirm(`¿Estás seguro de que querés eliminar este ${tipo}?`)) return false;

    $.ajax({
        type: "POST",
        url: "/Transacciones/EliminarTransaccion",
        data: { Id: id, Tipo: tipo },
        success: function () {
            mostrarToast(`🗑️ ${tipo.charAt(0).toUpperCase() + tipo.slice(1)} eliminado correctamente.`, "success");
            setTimeout(() => location.reload(), 2000);
        },
        error: function (xhr) {
            mostrarToast("❌ Error al eliminar: " + xhr.responseText, "error");
        }
    });

    return false;
}

function mostrarToast(mensaje, tipo = 'success') {
    const toast = document.createElement('div');
    toast.className = `toast-message toast-${tipo}`;
    toast.textContent = mensaje;

    const container = document.getElementById('toastContainer');
    container.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, 4000);
}

function animarNumero(id, valorFinal, duracion = 1000) {
    const el = document.getElementById(id);
    let inicio = null;
    const valorInicial = 0;

    function animar(timestamp) {
        if (!inicio) inicio = timestamp;
        const progreso = timestamp - inicio;
        const porcentaje = Math.min(progreso / duracion, 1);
        const valorActual = Math.floor(valorInicial + (valorFinal - valorInicial) * porcentaje);
        el.innerText = valorActual.toLocaleString();
        if (porcentaje < 1) requestAnimationFrame(animar);
    }

    requestAnimationFrame(animar);
}

document.addEventListener("DOMContentLoaded", () => {
    animarNumero("balanceTotal", 160000);
    animarNumero("ingresosTotales", 250000);
    animarNumero("gastosTotales", 90000);
});



