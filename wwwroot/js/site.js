// Confirmation de suppression
function confirmDelete(event) {
    if (!confirm('Êtes-vous sûr de vouloir supprimer ce produit ?')) {
        event.preventDefault();
        return false;
    }
    return true;
}

// Auto-dismiss des alertes
setTimeout(function () {
    var alerts = document.querySelectorAll('.alert');
    alerts.forEach(function (alert) {
        var bsAlert = new bootstrap.Alert(alert);
        bsAlert.close();
    });
}, 5000);

// Validation des formulaires
document.addEventListener('DOMContentLoaded', function () {
    var forms = document.querySelectorAll('.needs-validation');
    Array.prototype.slice.call(forms).forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
});