/**
 * MaintenanceHistory module
 * Manages the maintenance history page with AJAX loading, filtering, pagination and export
 */
var MaintenanceHistory = (function () {
  // Private variables
  var config = {
    tableContainerId: 'tableContainer',
    formId: 'filterForm',
    filterInputSelector: 'select, input[type="date"]',
    searchInputId: 'GlobalSearch',
    exportContainerId: 'exportButtons',
    pageNumberId: 'PageNumber',
    pageSizeId: 'PageSize',
    resetBtnId: 'resetFilterBtn',
    getTableUrl: '',
    exportTitle: 'Maintenance History'
  };

  var dataTable = null;
  var searchTimeout = null;

  // Private methods
  /**
   * Initialize DataTable for export functionality
   */
  function initDataTable() {
    // Destroy previous instance if exists
    if (dataTable) {
      dataTable.destroy();
    }

    dataTable = $('#maintenanceHistoryTable').DataTable({
      dom: 'Bfrtip',
      paging: false,
      ordering: false,
      info: false,
      searching: false,
      buttons: [
        {
          extend: 'excel',
          text: 'Excel',
          className: 'hidden-button',
          title: config.exportTitle,
          exportOptions: {
            columns: ':not(:last-child)'
          }
        },
        {
          extend: 'pdf',
          text: 'PDF',
          className: 'hidden-button',
          title: config.exportTitle,
          exportOptions: {
            columns: ':not(:last-child)'
          },
          customize: function (doc) {
            doc.content[1].table.widths = Array(doc.content[1].table.body[0].length + 1)
              .join('*')
              .split('');
            doc.defaultStyle.fontSize = 10;
            doc.styles.tableHeader.fontSize = 11;
            doc.styles.title.fontSize = 14;
            doc.footer = function (currentPage, pageCount) {
              return { text: currentPage.toString() + ' dari ' + pageCount, alignment: 'center' };
            };
          }
        }
      ]
    });

    return dataTable;
  }

  /**
   * Create and add export buttons
   */
  function setupExportButtons() {
    // Clear previous buttons
    $('#' + config.exportContainerId).empty();

    // Create new buttons
    var excelBtn = $('<a href="#" class="btn btn-sm btn-success"><i class="bx bx-file me-1"></i> Excel</a>');
    var pdfBtn = $('<a href="#" class="btn btn-sm btn-danger"><i class="bx bx-file me-1"></i> PDF</a>');

    // Add to container
    $('#' + config.exportContainerId)
      .append(excelBtn)
      .append(pdfBtn);

    // Add click handlers
    excelBtn.on('click', function (e) {
      e.preventDefault();
      if (dataTable) {
        dataTable.button(0).trigger(); // Excel
      }
    });

    pdfBtn.on('click', function (e) {
      e.preventDefault();
      if (dataTable) {
        dataTable.button(1).trigger(); // PDF
      }
    });

    // Hide DataTables buttons
    $('<style>.hidden-button, .dt-buttons { display: none !important; }</style>').appendTo('head');
  }

  /**
   * Load table data via AJAX
   */
  function loadTableData() {
    var formData = $('#' + config.formId).serialize();
    console.log('Loading table with params:', formData);

    // Show loading indicator
    $('#tableLoadingOverlay').removeClass('d-none');

    $.ajax({
      url: config.getTableUrl,
      type: 'GET',
      data: formData,
      cache: false,
      success: function (response) {
        // Replace table container with new HTML
        $('#' + config.tableContainerId).html(response);

        // Initialize DataTable for export after content is loaded
        initDataTable();

        // Update URL to allow bookmarking state
        updateBrowserUrl();
      },
      error: function (xhr, status, error) {
        // Show error message
        $('#' + config.tableContainerId).html(
          '<div class="alert alert-danger m-3">' +
            '<i class="bx bx-error-circle me-2"></i>' +
            'Terjadi kesalahan saat memuat data. Silakan coba lagi.' +
            '<br><small class="text-muted">' +
            (xhr.responseText || error) +
            '</small>' +
            '</div>'
        );
        console.error('AJAX Error:', status, error);
      },
      complete: function () {
        // Hide loading indicator
        $('#tableLoadingOverlay').addClass('d-none');
      }
    });
  }

  /**
   * Update browser URL without page reload
   */
  function updateBrowserUrl() {
    if (window.history && window.history.pushState) {
      var formData = $('#' + config.formId).serialize();
      var baseUrl = window.location.href.split('?')[0];
      var newUrl = baseUrl + (formData ? '?' + formData : '');
      window.history.pushState({ path: newUrl }, '', newUrl);
    }
  }

  /**
   * Reset form and load fresh data
   */
  function resetFilters() {
    // Reset form
    document.getElementById(config.formId).reset();

    // Reset hidden fields
    $('#' + config.pageNumberId).val(1);

    // Load fresh data
    loadTableData();
  }

  /**
   * Initialize event handlers
   */
  function bindEvents() {
    // Table row click for navigation
    $(document).on('click', '.clickable-row', function (e) {
      if (!$(e.target).closest('.action-buttons-cell').length) {
        window.location = $(this).data('url');
      }
    });

    // Pagination click
    $(document).on('click', '.pagination-group a', function (e) {
      e.preventDefault();

      if ($(this).hasClass('disabled') || $(this).attr('aria-disabled') === 'true') {
        return false;
      }

      var page = $(this).data('page');
      $('#' + config.pageNumberId).val(page);
      loadTableData();
    });

    // Page size change
    $(document).on('change', '#pageSizeSelector', function () {
      $('#' + config.pageSizeId).val($(this).val());
      $('#' + config.pageNumberId).val(1); // Reset to page 1
      loadTableData();
    });

    // Filter inputs change
    $(document).on('change', config.filterInputSelector, function () {
      $('#' + config.pageNumberId).val(1); // Reset to page 1
      loadTableData();
    });

    // Search input with debounce
    $(document).on('keyup', '#' + config.searchInputId, function () {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(function () {
        $('#' + config.pageNumberId).val(1); // Reset to page 1
        loadTableData();
      }, 500);
    });

    // Reset button
    $('#' + config.resetBtnId).on('click', function (e) {
      e.preventDefault();
      resetFilters();
    });
  }

  // Public API
  return {
    /**
     * Initialize the maintenance history page
     * @param {Object} options - Configuration options
     */
    init: function (options) {
      // Merge options with defaults
      config = $.extend(config, options || {});

      // Initialize export buttons
      setupExportButtons();

      // Initial DataTable setup
      initDataTable();

      // Bind events
      bindEvents();

      // Auto-close alerts after 5 seconds
      setTimeout(function () {
        $('.auto-close-alert').fadeOut('slow', function () {
          $(this).remove();
        });
      }, 5000);
    },

    /**
     * Manually reload the table data
     */
    reloadTable: function () {
      loadTableData();
    }
  };
})();
