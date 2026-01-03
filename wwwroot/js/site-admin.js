(function(){
    function disableOnSubmit() {
        document.querySelectorAll('form.disable-on-submit').forEach(function(form){
            form.addEventListener('submit', function(){
                var buttons = form.querySelectorAll('button[type=submit]');
                buttons.forEach(function(b){
                    b.disabled = true;
                    b.classList.add('disabled');
                });
            });
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', disableOnSubmit);
    else disableOnSubmit();
})();
