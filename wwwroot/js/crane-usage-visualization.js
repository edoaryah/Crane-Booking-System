$(document).ready(function () {
  const filterForm = $('#filterForm');
  const craneIdSelect = $('#CraneId');
  const dateInput = $('#Date');

  // Fungsi untuk mengirim form jika kedua input valid
  function autoSubmitFilter() {
    if (craneIdSelect.val() && dateInput.val()) {
      filterForm.submit();
    }
  }

  // Tambahkan event listener ke kedua input
  craneIdSelect.on('change', autoSubmitFilter);
  dateInput.on('change', autoSubmitFilter);
});
