/**
 * BillingHistory module - Complete Implementation
 * Manages the billing history page with AJAX loading, filtering, pagination and export
 */
var BillingHistory = (function () {
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
    exportTitle: 'Penagihan Booking Crane',
    companyName: 'PT. KALTIM PRIMA COAL',
    currentUser: 'System',
    companyLogo: '', // Base64 atau SVG string
    logoType: 'svg' // 'svg' atau 'image'
  };

  var dataTable = null;
  var searchTimeout = null;

  // ===== LOGO HANDLING FUNCTIONS =====
  /**
   * Set company logo dengan format yang benar
   */
  function setCompanyLogo(logoData) {
    if (!logoData) {
      config.companyLogo = '';
      config.logoType = null;
      return;
    }

    // Jika sudah berupa data URI
    if (logoData.startsWith('data:')) {
      config.companyLogo = logoData;
      config.logoType = 'image';
    }
    // Jika berupa base64 SVG (seperti yang user punya)
    else if (logoData.startsWith('PHN2Zy')) {
      try {
        config.companyLogo = atob(logoData); // Decode ke SVG string
        config.logoType = 'svg';
      } catch (error) {
        console.error('Error decoding SVG base64:', error);
        config.companyLogo = 'data:image/svg+xml;base64,' + logoData;
        config.logoType = 'image';
      }
    }
    // Jika berupa SVG string langsung
    else if (logoData.includes('<svg')) {
      config.companyLogo = logoData;
      config.logoType = 'svg';
    }
    // Default: treat as base64 image
    else {
      config.companyLogo = 'data:image/png;base64,' + logoData;
      config.logoType = 'image';
    }

    console.log('Logo set successfully. Type:', config.logoType);
  }

  // ===== PDF FUNCTIONS =====
  /**
   * Get current filter information for PDF header
   */
  function getFilterInfo() {
    var crane = $('#CraneId option:selected').text();
    var startDate = $('#StartDate').val();
    var endDate = $('#EndDate').val();
    var department = $('#Department option:selected').text();
    var status = $('#IsBilled option:selected').text();
    var search = $('#GlobalSearch').val();
    var filterParts = [];

    if (crane && crane !== '-- Semua Crane --') {
      filterParts.push('Crane: ' + crane);
    }

    if (startDate && endDate) {
      filterParts.push('Periode: ' + startDate + ' s/d ' + endDate);
    } else if (startDate) {
      filterParts.push('Dari: ' + startDate);
    } else if (endDate) {
      filterParts.push('Sampai: ' + endDate);
    }

    if (department && department !== '-- Semua Departemen --') {
      filterParts.push('Departemen: ' + department);
    }

    if (status && status !== '-- Semua Status --') {
      filterParts.push('Status: ' + status);
    }

    if (search) {
      filterParts.push('Pencarian: "' + search + '"');
    }

    return filterParts.length > 0 ? filterParts.join(' | ') : 'Semua Data';
  }

  /**
   * Create proper document definition sesuai dokumentasi pdfMake
   */
  function createDocumentDefinition(tableData) {
    var currentFilterInfo = getFilterInfo();
    var currentDate = new Date().toLocaleDateString('id-ID', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });

    // Document definition object sesuai dokumentasi pdfMake
    var docDefinition = {
      // Page configuration
      pageSize: 'A4',
      pageOrientation: 'landscape', // Landscape untuk table yang lebih lebar
      pageMargins: [40, config.companyLogo ? 100 : 80, 40, 60],

      // Document metadata
      info: {
        title: config.exportTitle,
        author: config.companyName,
        subject: 'Laporan Penagihan Booking Crane',
        creator: 'Crane Booking System'
      },

      // Header function (dynamic)
      header: function (currentPage, pageCount, pageSize) {
        var headerContent = {
          columns: [],
          margin: [40, 20, 40, 20]
        };

        // Logo column (jika ada)
        if (config.companyLogo) {
          if (config.logoType === 'svg') {
            headerContent.columns.push({
              svg: config.companyLogo,
              width: 80,
              height: 45
            });
          } else {
            headerContent.columns.push({
              image: config.companyLogo,
              width: 80,
              height: 45
            });
          }

          // Spacer column
          headerContent.columns.push({
            width: '*',
            text: ''
          });
        }

        // Page info column
        headerContent.columns.push({
          width: 'auto',
          stack: [
            {
              text: 'Hal. ' + currentPage + ' dari ' + pageCount,
              style: 'pageInfo'
            },
            {
              text: config.exportTitle,
              style: 'documentTitle'
            },
            {
              text: 'Crane Booking System',
              style: 'companySubtitle',
              margin: [0, 5, 0, 0]
            }
          ],
          alignment: 'right'
        });

        return headerContent;
      },

      // Footer function (dynamic)
      footer: function (currentPage, pageCount) {
        return {
          columns: [
            {
              text: 'Dicetak pada: ' + currentDate,
              style: 'footerLeft'
            },
            {
              text: 'Dicetak oleh: ' + config.currentUser,
              style: 'footerRight',
              alignment: 'right'
            }
          ],
          margin: [40, 10, 40, 0]
        };
      },

      // Main content
      content: [
        // Filter information
        {
          text: 'Filter: ' + currentFilterInfo,
          style: 'filterInfo',
          margin: [0, 0, 0, 15]
        },

        // Main table
        {
          style: 'tableExample',
          table: {
            headerRows: 1,
            widths: [30, 'auto', 'auto', '*', '*', 'auto', 'auto'],
            body: tableData
          },
          layout: {
            fillColor: function (rowIndex, node, columnIndex) {
              if (rowIndex === 0) {
                return '#3498db'; // Blue header background
              }
              return rowIndex % 2 === 0 ? null : '#f8f9fa';
            }
          }
        }
      ],

      // Default style
      defaultStyle: {
        fontSize: 9,
        color: '#2c3e50'
      },

      // Style dictionary
      styles: {
        companyName: {
          fontSize: 12,
          bold: true,
          color: '#2c3e50'
        },
        companySubtitle: {
          fontSize: 10,
          color: '#7f8c8d',
          italics: true
        },
        documentTitle: {
          fontSize: 16,
          bold: true,
          color: '#2c3e50'
        },
        pageInfo: {
          fontSize: 8,
          color: '#95a5a6'
        },
        filterInfo: {
          fontSize: 10,
          italics: true,
          color: '#7f8c8d',
          background: '#ecf0f1'
        },
        tableExample: {
          margin: [0, 5, 0, 15]
        },
        tableHeader: {
          bold: true,
          fontSize: 10,
          color: 'white',
          alignment: 'center'
        },
        tableHeaderCenter: {
          bold: true,
          fontSize: 10,
          color: 'white',
          alignment: 'center'
        },
        tableCell: {
          fontSize: 9,
          color: '#2c3e50'
        },
        tableCellCenter: {
          fontSize: 9,
          color: '#2c3e50',
          alignment: 'center'
        },
        tableCellRight: {
          fontSize: 9,
          color: '#2c3e50',
          alignment: 'right'
        },
        footerLeft: {
          fontSize: 8,
          color: '#95a5a6'
        },
        footerRight: {
          fontSize: 8,
          color: '#95a5a6'
        }
      }
    };

    return docDefinition;
  }

  /**
   * Process table data untuk PDF export
   */
  function processTableDataForPDF() {
    var tableData = [];
    var $table = $('#billingTable');

    if ($table.length === 0) {
      console.warn('Table not found for PDF export');
      return [['Tidak ada data untuk diekspor']];
    }

    // Process header (tidak berubah)
    var headerRow = [];
    $table.find('thead th').each(function (index) {
      var $th = $(this);

      if (!$th.hasClass('action-column') && !$th.text().toLowerCase().includes('actions')) {
        var headerText = $th.text().trim();

        if (index === 0) {
          headerRow.push({
            text: 'No.',
            style: 'tableHeaderCenter'
          });
        } else {
          headerRow.push({
            text: headerText,
            style: 'tableHeader'
          });
        }
      }
    });

    if (headerRow.length > 0) {
      tableData.push(headerRow);
    }

    // Process body rows
    $table.find('tbody tr').each(function () {
      var $row = $(this);
      var rowData = [];

      $row.find('td').each(function (index) {
        var $td = $(this);

        if (!$td.hasClass('action-buttons-cell')) {
          var cellText = $td.text().trim();

          // KHUSUS UNTUK KOLOM TANGGAL AKTUAL (index 5)
          // Hilangkan text yang mengandung "Booking:"
          if (index === 5) {
            // Split berdasarkan baris dan filter yang tidak mengandung "Booking:"
            var lines = cellText.split('\n');
            var filteredLines = [];

            for (var i = 0; i < lines.length; i++) {
              var line = lines[i].trim();
              // Hanya ambil baris yang TIDAK mengandung "Booking:"
              if (line && !line.toLowerCase().includes('booking:')) {
                filteredLines.push(line);
              }
            }

            cellText = filteredLines.join(' ').trim();
          }

          // Clean up whitespace
          cellText = cellText.replace(/\s+/g, ' ').trim();

          if (index === 0) {
            rowData.push({
              text: cellText,
              style: 'tableCellCenter'
            });
          } else {
            rowData.push(cellText);
          }
        }
      });

      if (rowData.length > 0) {
        tableData.push(rowData);
      }
    });

    if (tableData.length <= 1) {
      tableData.push([
        {
          text: 'Tidak ada data yang tersedia',
          colSpan: headerRow.length || 7,
          alignment: 'center',
          style: 'tableCell'
        }
      ]);
    }

    return tableData;
  }

  // ===== DATATABLE FUNCTIONS =====
  /**
   * Initialize DataTable for export functionality
   */
  function initDataTable() {
    // Destroy previous instance if exists
    if (dataTable) {
      dataTable.destroy();
    }

    dataTable = $('#billingTable').DataTable({
      dom: 'Bfrtip',
      paging: false,
      ordering: false,
      info: false,
      searching: false,
      buttons: [
        {
          extend: 'excelHtml5',
          text: '<i class="bx bx-file me-1"></i> Excel',
          className: 'btn btn-sm btn-success hidden-button',
          title: config.exportTitle,
          exportOptions: {
            columns: ':not(.action-column):not(:last-child)'
          }
        },
        {
          extend: 'pdfHtml5',
          text: '<i class="bx bx-file me-1"></i> PDF',
          className: 'btn btn-sm btn-danger hidden-button',
          title: config.exportTitle,
          exportOptions: {
            columns: ':not(.action-column):not(:last-child)'
          },
          customize: function (doc) {
            try {
              var tableData = processTableDataForPDF();
              var newDocDef = createDocumentDefinition(tableData);

              // Replace the entire doc object properties
              Object.keys(doc).forEach(function (key) {
                delete doc[key];
              });

              Object.keys(newDocDef).forEach(function (key) {
                doc[key] = newDocDef[key];
              });

              console.log('PDF document definition created successfully');
            } catch (error) {
              console.error('Error in PDF customize function:', error);

              // Fallback: minimal PDF structure
              doc.content = [
                {
                  text: config.exportTitle,
                  style: { fontSize: 16, bold: true, alignment: 'center', margin: [0, 0, 0, 20] }
                },
                {
                  text: 'Terjadi kesalahan dalam pembuatan PDF. Data mungkin tidak lengkap.',
                  style: { fontSize: 12, alignment: 'center', color: 'red' }
                }
              ];
            }
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

    // Create button group container
    var buttonGroup = $('<div class="btn-group" role="group"></div>');

    // Create buttons
    var excelBtn = $(
      '<button type="button" class="btn btn-sm btn-success">' + '<i class="bx bx-file me-1"></i> Excel</button>'
    );
    var pdfBtn = $(
      '<button type="button" class="btn btn-sm btn-danger">' + '<i class="bx bx-file me-1"></i> PDF</button>'
    );

    // Add to button group
    buttonGroup.append(excelBtn).append(pdfBtn);

    // Add to container
    $('#' + config.exportContainerId).append(buttonGroup);

    // Add click handlers
    excelBtn.on('click', function (e) {
      e.preventDefault();
      if (dataTable) {
        try {
          dataTable.button(0).trigger(); // Excel
        } catch (error) {
          console.error('Excel export error:', error);
          showExportError('Excel', error.message);
        }
      }
    });

    pdfBtn.on('click', function (e) {
      e.preventDefault();
      if (dataTable) {
        try {
          if (typeof window.pdfMake === 'undefined') {
            throw new Error('pdfMake library is not loaded');
          }

          dataTable.button(1).trigger(); // PDF
        } catch (error) {
          console.error('PDF export error:', error);
          showExportError('PDF', error.message);
        }
      }
    });

    // Enhanced CSS for buttons
    if (!$('#datatables-export-style').length) {
      $(
        '<style id="datatables-export-style">' +
          '.hidden-button, .dt-buttons { display: none !important; }' +
          '.btn-group .btn { border-radius: 0; }' +
          '.btn-group .btn:first-child { border-top-left-radius: 0.375rem; border-bottom-left-radius: 0.375rem; }' +
          '.btn-group .btn:last-child { border-top-right-radius: 0.375rem; border-bottom-right-radius: 0.375rem; }' +
          '.btn-group .btn:hover { transform: translateY(-1px); box-shadow: 0 2px 4px rgba(0,0,0,0.1); }' +
          '#billingTable thead tr:first-child th { border-top: 1px solid #B2B2B2 !important; }' +
          '#billingTable tbody tr:last-child td { border-bottom: none !important; }' +
          '</style>'
      ).appendTo('head');
    }
  }

  /**
   * Show export error message
   */
  function showExportError(exportType, errorMessage) {
    var errorHtml =
      '<div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">' +
      '<i class="bx bx-error-circle me-2"></i>' +
      '<strong>Export ' +
      exportType +
      ' Gagal!</strong><br>' +
      'Terjadi kesalahan: ' +
      errorMessage +
      '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
      '</div>';

    var $container = $('#' + config.exportContainerId).parent();
    if ($container.length === 0) {
      $container = $('#' + config.tableContainerId);
    }

    $container.prepend(errorHtml);

    // Auto remove after 10 seconds
    setTimeout(function () {
      $container.find('.alert-danger').fadeOut('slow', function () {
        $(this).remove();
      });
    }, 10000);
  }

  // ===== AJAX & UI FUNCTIONS =====
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
        setTimeout(function () {
          try {
            initDataTable();
          } catch (error) {
            console.error('DataTable initialization error:', error);
          }
        }, 100);

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

  // ===== PUBLIC API =====
  return {
    /**
     * Initialize the billing history page
     */
    init: function (options) {
      // Merge options with defaults
      config = $.extend(config, options || {});

      // Check required dependencies
      if (typeof window.pdfMake === 'undefined') {
        console.warn('pdfMake is not loaded. PDF export may not work.');
      }

      // Initialize export buttons
      setupExportButtons();

      // Initial DataTable setup
      try {
        initDataTable();
      } catch (error) {
        console.error('Initial DataTable setup error:', error);
      }

      // Bind events
      bindEvents();

      // Auto-close alerts after 5 seconds
      setTimeout(function () {
        $('.auto-close-alert').fadeOut('slow', function () {
          $(this).remove();
        });
      }, 5000);

      console.log('BillingHistory initialized successfully');
    },

    /**
     * Manually reload the table data
     */
    reloadTable: function () {
      loadTableData();
    },

    /**
     * Update company logo
     */
    setCompanyLogo: function (logoData) {
      setCompanyLogo(logoData);
    },

    /**
     * Update company name
     */
    setCompanyName: function (companyName) {
      config.companyName = companyName;
    },

    /**
     * Get current configuration
     */
    getConfig: function () {
      return config;
    },

    /**
     * Test PDF generation (untuk debugging)
     */
    testPDFGeneration: function () {
      try {
        var tableData = processTableDataForPDF();
        var docDef = createDocumentDefinition(tableData);

        console.log('PDF Document Definition:', docDef);

        if (window.pdfMake) {
          window.pdfMake.createPdf(docDef).download('test-billing-history.pdf');
        } else {
          console.error('pdfMake is not available');
        }
      } catch (error) {
        console.error('Test PDF generation failed:', error);
      }
    }
  };
})();
