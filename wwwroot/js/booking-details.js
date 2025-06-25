/* ------------------ New BookingDetails Implementation ------------------ */
var BookingDetails = (function () {
  var config = {
    exportTitle: 'MATERIAL HANDLING SERVICE FORM',
    companyName: 'PT. KALTIM PRIMA COAL',
    currentUser: 'System',
    companyLogo: '', // data URI or svg markup
    logoType: null,
    showLogoPreview: false
  };

  // Helper: safe text extraction
  function getInputVal(selector) {
    var el = document.querySelector(selector);
    if (!el) return '';
    if (typeof el.value !== 'undefined') return (el.value || '').trim();
    return (el.getAttribute('value') || '').trim();
  }
  function getText(selector) {
    var el = document.querySelector(selector);
    if (!el) return '-';
    // if input/textarea, use value; otherwise textContent
    if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') {
      return (el.value || '').trim() || '-';
    }
    return el.textContent.trim() || '-';
  }

  // Attach semantic classes to existing readonly inputs so PDF can find them
  document.addEventListener('DOMContentLoaded', function () {
    try {
      // map label text -> desired class
      var mapping = {
        'Requestor Name': 'requestor-name',
        'Department Name': 'department-name',
        'Start Date': 'request-date' // use start date as request date proxy
      };
      Object.keys(mapping).forEach(function (labelText) {
        var label = Array.from(document.querySelectorAll('label')).find(function (l) {
          return l.textContent.trim() === labelText;
        });
        if (label) {
          var input = label.parentElement.querySelector('input, textarea');
          if (input) input.classList.add(mapping[labelText]);
        }
      });
    } catch (e) {
      console.warn('Failed to tag elements for PDF export', e);
    }
  });

  // Public helper: set company logo (similar to old implementation)
  function setCompanyLogo(logoData) {
    if (!logoData) return;

    // 1. Normalise logo data & detect type
    if (logoData.startsWith('data:')) {
      config.companyLogo = logoData;
      config.logoType = 'image';
    } else if (logoData.startsWith('PHN2Zy')) {
      // base64-encoded SVG
      try {
        config.companyLogo = atob(logoData);
        config.logoType = 'svg';
      } catch (e) {
        console.error('Cannot decode SVG base64', e);
        config.companyLogo = 'data:image/svg+xml;base64,' + logoData;
        config.logoType = 'image';
      }
    } else if (logoData.includes('<svg')) {
      // raw SVG markup
      config.companyLogo = logoData;
      config.logoType = 'svg';
    } else {
      // assume raw base64 PNG/JPG
      config.companyLogo = 'data:image/png;base64,' + logoData;
      config.logoType = 'image';
    }

    // 2. Optionally render / update logo preview on page (disabled by default)
    if (!config.showLogoPreview) return; // skip DOM injection unless explicitly enabled
    // 2a. Render / update logo preview on page
    try {
      var header = document.querySelector('.card-header');
      if (!header) return; // nothing to attach to

      // ensure container exists
      var container = document.getElementById('companyLogoContainer');
      if (!container) {
        container = document.createElement('div');
        container.id = 'companyLogoContainer';
        container.style.marginBottom = '6px';
        header.prepend(container);
      }

      // create or reuse img element
      var img = container.querySelector('img');
      if (!img) {
        img = document.createElement('img');
        img.style.maxHeight = '90px';
        img.style.display = 'block';
        container.appendChild(img);
      }

      // decide src value
      if (config.logoType === 'svg') {
        // If stored as raw SVG markup, convert to data URI for <img>
        if (config.companyLogo.startsWith('<svg')) {
          img.src = 'data:image/svg+xml;base64,' + btoa(config.companyLogo);
        } else {
          img.src = config.companyLogo; // already data URI
        }
      } else {
        img.src = config.companyLogo;
      }
    } catch (err) {
      console.warn('Failed to render company logo preview', err);
    }
  }

  /*
   * First table: Header row + 2 approval rows (requestor & manager)
   * Structure:
   * | (blank) | Nama | Departemen/ Kontraktor | Tanda Tangan | Tanggal |
   * | Diminta oleh Pemohon | ... |
   * | Diverifikasi & disetujui oleh Manager KPC / Kustodian Kontrak | ... |
   */
  function buildApprovalTable() {
    // === collect text ===
    var requestor = getText('.requestor-name');
    var dept = getText('.department-name');
    var reqDate = getText('.request-date');
    // fallback to timeline date-time if only date present
    if (!reqDate || /^\d{4}-\d{2}-\d{2}$/.test(reqDate)) {
      var firstTimelineTs = document.querySelector('.timeline-item:first-child .timeline-header small');
      if (firstTimelineTs) {
        reqDate = firstTimelineTs.textContent.trim();
      }
    }

    var manager = getText('.manager-name');
    if (!manager) {
      // additional fallback: find timeline item whose header contains 'Manager'
      document.querySelectorAll('.timeline-item').forEach(function (item) {
        var hdr = item.querySelector('.timeline-header h6');
        if (hdr && /Manager/i.test(hdr.textContent)) {
          var p = item.querySelector('.timeline-body p');
          if (p && p.textContent.trim()) {
            manager = p.textContent.trim();
          }
        }
      });
      var managerTimelineBody = document.querySelectorAll('.timeline-item .timeline-body p');
      if (managerTimelineBody.length > 1) {
        manager = managerTimelineBody[1].textContent.trim();
      }
    }
    var managerDept = getText('.manager-department-name');
    if (!managerDept || managerDept === '-') {
      managerDept = getText('.manager-dept');
    }
    if (!managerDept || managerDept === '-') {
      managerDept = dept;
    }
    var mgrDate = getText('.manager-date');
    if (!mgrDate) {
      // additional fallback from timeline search
      document.querySelectorAll('.timeline-item').forEach(function (item) {
        var hdr = item.querySelector('.timeline-header h6');
        if (hdr && /Manager/i.test(hdr.textContent)) {
          var sm = item.querySelector('.timeline-header small');
          if (sm && sm.textContent.trim()) {
            mgrDate = sm.textContent.trim();
          }
        }
      });
      var mgrDateSmall = document.querySelectorAll('.timeline-item .timeline-header small');
      if (mgrDateSmall.length > 1) {
        mgrDate = mgrDateSmall[1].textContent.trim();
      }
    }
    if (!mgrDate || /^\d{4}-\d{2}-\d{2}$/.test(mgrDate)) {
      var timelineSmalls = document.querySelectorAll('.timeline-item .timeline-header small');
      if (timelineSmalls.length > 1) {
        mgrDate = timelineSmalls[1].textContent.trim();
      }
    }

    return {
      style: 'tableBlock',
      table: {
        headerRows: 1,
        widths: [150, '*', '*', 90],
        body: [
          // Title row with left & right logos
          (function () {
            var leftLogo = '';
            var rightLogo = '';
            if (config.companyLogo) {
              if (config.logoType === 'svg') {
                leftLogo = { svg: config.companyLogo, width: 80, margin: [0, 5, 0, 5] };
                // clone object so pdfmake doesn't reuse same reference
                rightLogo = { svg: config.companyLogo, width: 80, margin: [0, 5, 0, 5], alignment: 'right' };
              } else {
                leftLogo = { image: config.companyLogo, width: 80, margin: [0, 5, 0, 5] };
                rightLogo = { image: config.companyLogo, width: 80, margin: [0, 5, 0, 5], alignment: 'right' };
              }
            }
            return [
              leftLogo,
              { text: 'MATERIAL HANDLING\nSERVICE FORM', style: 'mainTitle', alignment: 'center', colSpan: 2 },
              {},
              rightLogo
            ];
          })(),
          // Subtitle row spanning all columns
          [
            {
              text: 'Permohonan ini harus dikirimkan ke Crane Base sebelum pekerjaan dilakukan',
              style: 'subTitle',
              alignment: 'center',
              colSpan: 4
            },
            '',
            '',
            ''
          ],
          [
            '',
            { text: 'Nama', style: 'tableHeader' },
            { text: 'Departemen atau Kontraktor', style: 'tableHeader' },
            { text: 'Tanggal', style: 'tableHeader' }
          ],
          [{ text: 'Diminta oleh Pemohon', style: 'rowLabel' }, requestor, dept, reqDate],
          [
            { text: 'Diverifikasi & disetujui oleh Manager KPC / Kustodian Kontrak', style: 'rowLabel' },
            manager,
            managerDept,
            mgrDate
          ]
        ]
      }
      // default layout draws full grid
      // remove custom layout to show both horizontal & vertical lines
    };
  }

  // Build combined header + data tables so header column widths differ
  function buildApprovalSection() {
    var requestor = getText('.requestor-name');
    var dept = getText('.department-name');
    var reqDate = getText('.request-date');
    // fallback to timeline date-time if only date present
    if (!reqDate || /^\d{4}-\d{2}-\d{2}$/.test(reqDate)) {
      var firstTimelineTs = document.querySelector('.timeline-item:first-child .timeline-header small');
      if (firstTimelineTs) {
        reqDate = firstTimelineTs.textContent.trim();
      }
    }
    var manager = getText('.manager-name');
    if (!manager) {
      // additional fallback: find timeline item whose header contains 'Manager'
      document.querySelectorAll('.timeline-item').forEach(function (item) {
        var hdr = item.querySelector('.timeline-header h6');
        if (hdr && /Manager/i.test(hdr.textContent)) {
          var p = item.querySelector('.timeline-body p');
          if (p && p.textContent.trim()) {
            manager = p.textContent.trim();
          }
        }
      });
      var managerTimelineBody = document.querySelectorAll('.timeline-item .timeline-body p');
      if (managerTimelineBody.length > 1) {
        manager = managerTimelineBody[1].textContent.trim();
      }
    }
    var managerDept = getText('.manager-department-name');
    if (!managerDept || managerDept === '-') {
      managerDept = getText('.manager-dept');
    }
    if (!managerDept || managerDept === '-') {
      managerDept = dept;
    }
    var mgrDate = getText('.manager-date');
    if (!mgrDate) {
      // additional fallback from timeline search
      document.querySelectorAll('.timeline-item').forEach(function (item) {
        var hdr = item.querySelector('.timeline-header h6');
        if (hdr && /Manager/i.test(hdr.textContent)) {
          var sm = item.querySelector('.timeline-header small');
          if (sm && sm.textContent.trim()) {
            mgrDate = sm.textContent.trim();
          }
        }
      });
      var mgrDateSmall = document.querySelectorAll('.timeline-item .timeline-header small');
      if (mgrDateSmall.length > 1) {
        mgrDate = mgrDateSmall[1].textContent.trim();
      }
    }
    if (!mgrDate || /^\d{4}-\d{2}-\d{2}$/.test(mgrDate)) {
      var timelineSmalls = document.querySelectorAll('.timeline-item .timeline-header small');
      if (timelineSmalls.length > 1) {
        mgrDate = timelineSmalls[1].textContent.trim();
      }
    }

    // symmetric column widths
    var headerWidths = ['*', 130, 130, '*'];
    var dataWidths = [150, '*', '*', 90];
    var leftLogo = '',
      rightLogo = '';
    if (config.companyLogo) {
      if (config.logoType === 'svg') {
        leftLogo = { svg: config.companyLogo, width: 80, margin: [0, 5, 0, 5] };
        rightLogo = { svg: config.companyLogo, width: 80, margin: [0, 5, 0, 5], alignment: 'right' };
      } else {
        leftLogo = { image: config.companyLogo, width: 80, margin: [0, 5, 0, 5] };
        rightLogo = { image: config.companyLogo, width: 80, margin: [0, 5, 0, 5], alignment: 'right' };
      }
    }

    var headerTable = {
      widths: headerWidths,
      body: [
        [
          leftLogo,
          { text: 'MATERIAL HANDLING\nSERVICE FORM', style: 'mainTitle', alignment: 'center', colSpan: 2 },
          {},
          rightLogo
        ],
        [
          {
            text: 'Permohonan ini harus dikirimkan ke Crane Base sebelum pekerjaan dilakukan',
            style: 'subTitle',
            alignment: 'center',
            colSpan: 4
          },
          '',
          '',
          ''
        ]
      ]
    };
    var dataTable = {
      headerRows: 1,
      widths: dataWidths,
      body: [
        [
          '',
          { text: 'Nama', style: 'tableHeader' },
          { text: 'Departemen atau Kontraktor', style: 'tableHeader' },
          { text: 'Tanggal', style: 'tableHeader' }
        ],
        [{ text: 'Diminta oleh Pemohon', style: 'rowLabel' }, requestor, dept, reqDate],
        [
          { text: 'Diverifikasi & disetujui oleh Manager KPC / Kustodian Kontrak', style: 'rowLabel' },
          manager,
          managerDept,
          mgrDate
        ]
      ]
    };
    var headerLayout = {
      hLineWidth: function () {
        return 1; // keep horizontal lines
      },
      vLineWidth: function (i, node) {
        // only draw outer vertical lines (left & right borders)
        return i === 0 || i === node.table.widths.length ? 1 : 0;
      }
    };
    var dataLayout = {
      hLineWidth: function (i) {
        return i === 0 ? 0 : 1;
      },
      vLineWidth: function () {
        return 1;
      }
    };

    // ===== Additional information table (Location, Supervisor, Cost Code, Phone) =====
    // helper to get value by label text if element classes not present
    function findInputValue(labelText) {
      var labels = document.querySelectorAll('label');
      for (var i = 0; i < labels.length; i++) {
        if (labels[i].textContent.trim().toLowerCase() === labelText.toLowerCase()) {
          // assume the input is next sibling or within same parent
          var input = labels[i].parentElement.querySelector('input, textarea');
          if (!input) {
            // try next sibling element
            input = labels[i].nextElementSibling;
          }
          if (input) {
            return (input.value || input.textContent || '').trim() || '-';
          }
        }
      }
      return '-';
    }

    var locationText = getText('.booking-location');
    if (locationText === '-') {
      locationText = getText('.location');
    }
    if (locationText === '-') {
      locationText = findInputValue('Location');
    }
    var supervisorText = getText('.project-supervisor');
    var costCodeText = getText('.cost-code');
    var phoneText = getText('.phone-number');

    if (supervisorText === '-') {
      supervisorText =
        findInputValue('Supv. Proyek') || findInputValue('Project Supervisor') || findInputValue('Supervisor Name');
    }
    if (costCodeText === '-') {
      costCodeText = findInputValue('Cost Code');
    }
    if (phoneText === '-') {
      phoneText = findInputValue('Phone Number');
    }

    // helper to build simple vector checkmark (no external font)
    function makePdfCheckmark() {
      return {
        svg: '<svg width="10" height="10" viewBox="0 0 12 12"><polyline points="1,6 4,10 11,1" stroke="#000" stroke-width="1.5" fill="none" /></svg>',
        width: 10,
        height: 10,
        alignment: 'center'
      };
    }

    // ===== Build Shift table from DOM =====
    var shiftPdfTable = null;
    var shiftLayout = null;
    var shiftTableElm = document.querySelector('#shiftTableContainer table');
    if (shiftTableElm) {
      var headerCells = Array.from(shiftTableElm.querySelectorAll('thead th'));
      if (headerCells.length > 1) {
        var headerRow = headerCells.map(function (th, idx) {
          var raw = th.innerText || th.textContent;
          var parts = raw
            .split(/\n|<br\s*\/?>/i)
            .map(function (p) {
              return p.trim();
            })
            .filter(Boolean);
          if (parts.length === 2) {
            return {
              stack: [
                { text: parts[0], bold: true, alignment: 'center' },
                { text: parts[1], fontSize: 8, alignment: 'center' }
              ],
              alignment: 'center'
            };
          }
          var obj = { text: raw.trim(), alignment: 'center', style: 'rowLabel' };
          return obj;
        });
        // ensure DATE column header visually centered with equal padding
        if (headerRow.length) {
          headerRow[0].margin = [0, 4, 0, 4];
        }
        var bodyRows = [];
        Array.from(shiftTableElm.querySelectorAll('tbody tr')).forEach(function (tr) {
          var cells = Array.from(tr.querySelectorAll('td'));
          var row = cells.map(function (td, idx) {
            if (idx === 0) {
              return td.textContent.trim();
            }
            var span = td.querySelector('span');
            var markObj = span && span.classList.contains('checked') ? makePdfCheckmark() : null;
            return markObj || { text: '-', alignment: 'center' };
          });
          bodyRows.push(row);
        });
        var widthsArr = [60].concat(
          headerCells.slice(1).map(function () {
            return '*';
          })
        );
        shiftPdfTable = { widths: widthsArr, body: [headerRow].concat(bodyRows) };
        shiftLayout = {
          hLineWidth: function () {
            return 1;
          },
          vLineWidth: function () {
            return 1;
          }
        };
      }
    }

    var infoTable = {
      widths: [70, 150],
      body: [
        [{ text: 'Lokasi :', style: 'rowLabel' }, locationText],
        [{ text: 'Supv. Proyek :', style: 'rowLabel' }, supervisorText],
        [{ text: 'Cost Code :', style: 'rowLabel' }, costCodeText],
        [{ text: 'Nomor Tlp :', style: 'rowLabel' }, phoneText]
      ]
    };
    var infoLayout = {
      hLineWidth: function () {
        return 1;
      },
      vLineWidth: function () {
        return 1;
      }
    };
    // ===== Job Description field =====
    var jobDescText = '';
    var descTextEl = document.querySelector('.job-description');
    if (descTextEl) {
      jobDescText = (descTextEl.value || descTextEl.textContent || '').trim();
    }
    if (!jobDescText) {
      jobDescText = findInputValue('Job Description');
      if (jobDescText === '-') jobDescText = '';
    }

    var jobDescTable = {
      widths: ['*'],
      body: [[{ text: jobDescText || ' ', margin: [4, 10, 4, 10] }]]
    };

    // ===== Service Details (Crane) =====
    var craneCode = getInputVal('.crane-code') || getText('.crane-code');
    var craneCap = getInputVal('.crane-capacity') || getText('.crane-capacity');
    var serviceText = '';
    if (craneCode && craneCode !== '-') {
      serviceText += craneCode;
    }
    if (craneCap && craneCap !== '-') {
      serviceText += (serviceText ? ' ' : '') + craneCap + ' TON';
    }

    // ===== Build service details table (crane code + capacity) =====
    var serviceTable = {
      widths: ['*'],
      body: [[{ text: serviceText || ' ', margin: [4, 10, 4, 10] }]]
    };

    // ===== Build items-to-be-lifted table =====
    var itemsBody = [];
    // header row
    itemsBody.push([
      { text: 'NO', style: 'tableHeader', alignment: 'center' },
      { text: 'Nama Item (L x W x H)', style: 'tableHeader', alignment: 'center' },
      { text: 'Tinggi Of Lifting (m)', style: 'tableHeader', alignment: 'center' },
      { text: 'Berat (Ton)', style: 'tableHeader', alignment: 'center' },
      { text: 'Jml', style: 'tableHeader', alignment: 'center' }
    ]);

    var itemRows = document.querySelectorAll('#itemsTableContainer tbody tr');
    var rowIndex = 1;
    itemRows.forEach(function (tr) {
      var cells = tr.querySelectorAll('td');
      if (cells.length >= 4) {
        var itemName = cells[0].textContent.trim();
        var height = cells[1].textContent.trim();
        var weight = cells[2].textContent.trim();
        var qty = cells[3].textContent.trim();
        itemsBody.push([
          { text: String(rowIndex), alignment: 'center' },
          itemName,
          { text: height, alignment: 'center' },
          { text: weight, alignment: 'center' },
          { text: qty, alignment: 'center' }
        ]);
        rowIndex++;
      }
    });
    // ensure at least 5 empty rows for printed form aesthetics if fewer items
    for (var i = itemsBody.length; i <= 5; i++) {
      itemsBody.push([' ', ' ', ' ', ' ', ' ']);
    }

    var itemsTable = {
      headerRows: 1,
      widths: [20, '*', '*', '*', '*'],
      body: itemsBody
    };

    // ===== Potential Hazards table =====
    var hazardsBody = [];
    hazardsBody.push([
      { text: 'NO', style: 'tableHeader', alignment: 'center' },
      { text: 'Potential Hazard', style: 'tableHeader', alignment: 'center' }
    ]);
    var hazardEls = document.querySelectorAll('.hazard-badge');
    var hzIndex = 1;
    hazardEls.forEach(function (el) {
      var hzText = el.textContent.trim();
      if (hzText) {
        hazardsBody.push([{ text: String(hzIndex), alignment: 'center' }, hzText]);
        hzIndex++;
      }
    });
    var customHazardEl = document.querySelector('#hazardsCardContainer .mt-3 p');
    if (customHazardEl && customHazardEl.textContent.trim()) {
      hazardsBody.push([{ text: String(hzIndex), alignment: 'center' }, customHazardEl.textContent.trim()]);
      hzIndex++;
    }
    // ensure at least 5 rows for consistent layout
    for (var h = hazardsBody.length; h <= 5; h++) {
      hazardsBody.push([' ', ' ']);
    }
    var hazardsTable = {
      headerRows: 1,
      widths: [30, '*'],
      body: hazardsBody
    };

    // ===== Catatan static notes =====
    var notesBody = [
      [makePdfCheckmark(), 'Pengguna harus menyiapkan akses yang aman ke area kerja dan tempat kerja yang aman. Untuk bekerja pada malam hari, pengguna harus menyediakan penerangan yang cukup memadahi.'],
      [makePdfCheckmark(), 'Pengguna harus melengkapi izin yang sudah disetujui. Misalnya izin bekerja di dekat saluran listrik, Akses PIT dll dan pengguna harus memandu bila diperlukan untuk ke lokasi pekerjaan dan membantu mengawasi pekerjaan yang sedang berlangsung dan juga pengguna harus menginformasikan secara tertulis jika ada perubahan akses atau jalur yang akan dilalui.'],
      [makePdfCheckmark(), 'Hanya pekerjaan yang sesuai dengan persyaratan yang akan diproses.']
    ];
    var notesTable = { widths: [20, '*'], body: notesBody };

    var descLayout = {
      hLineWidth: function () {
        return 1;
      },
      vLineWidth: function () {
        return 1;
      }
    };

    return {
      margin: [0, 6, 0, 12],
      stack: [
        { table: headerTable, layout: headerLayout },
        { table: dataTable, layout: dataLayout },
        shiftPdfTable
          ? {
              columns: [
                {
                  width: 239,
                  stack: [
                    { table: infoTable, layout: infoLayout },
                    { text: 'URAIAN PEKERJAAN YANG AKAN DILAKUKAN', style: 'rowLabel', margin: [0, 12, 0, 4] },
                    { table: jobDescTable, layout: descLayout },
                    { text: 'RINCIAN LAYANAN YANG DIBUTUHKAN', style: 'rowLabel', margin: [0, 12, 0, 4] },
                    { table: serviceTable, layout: descLayout }
                  ]
                },
                { width: '*', table: shiftPdfTable, layout: shiftLayout }
              ],
              columnGap: 10,
              margin: [0, 10, 0, 0]
            }
          : [
              { table: infoTable, layout: infoLayout, margin: [0, 10, 0, 0] },
              { text: 'URAIAN PEKERJAAN YANG AKAN DILAKUKAN', style: 'rowLabel', margin: [0, 12, 0, 4] },
              { table: jobDescTable, layout: descLayout },
              { text: 'RINCIAN LAYANAN YANG DIBUTUHKAN', style: 'rowLabel', margin: [0, 12, 0, 4] },
              { table: serviceTable, layout: descLayout }
            ],
        { text: 'ITEMS TO BE LIFTED', style: 'rowLabel', margin: [0, 12, 0, 4] },
        { table: itemsTable, layout: descLayout },
        { text: 'POTENTIAL HAZARDS', style: 'rowLabel', margin: [0, 12, 0, 4] },
        { table: hazardsTable, layout: descLayout },
        // ===== Area / Item Photos =====
        (function () {
          var images = document.querySelectorAll('.booking-image-gallery img');
          if (!images.length) return null;
          var photosBody = [];
          var row = [];
          images.forEach(function (img, idx) {
            var src = img.getAttribute('data-src-base64') || img.src;
            if (!src.startsWith('data:')) {
              try {
                var canvas = document.createElement('canvas');
                canvas.width = img.naturalWidth || img.width;
                canvas.height = img.naturalHeight || img.height;
                var ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0);
                src = canvas.toDataURL('image/png');
              } catch (e) {
                console.warn('Could not convert image to base64', e);
                src = null;
              }
            }
            var cell = src ? { image: src, fit: [160, 120], alignment: 'center', margin: [2, 2, 2, 2] } : ' ';
            row.push(cell);
            if ((idx + 1) % 3 === 0) {
              photosBody.push(row);
              row = [];
            }
          });
          if (row.length) {
            // pad remaining cells to 3 columns
            while (row.length < 3) row.push(' ');
            photosBody.push(row);
          }
          var photosTable = { widths: ['*', '*', '*'], body: photosBody };
          return [
            { text: 'AREA / ITEM PHOTOS', style: 'rowLabel', margin: [0, 12, 0, 4] },
            { table: photosTable, layout: descLayout }
          ];
          var images = document.querySelectorAll('.booking-image-gallery img');
          if (!images.length) return null;
          var photoBlocks = [];
          var cols = [];
          var processed = 0;
          images.forEach(function (img) {
            var src = img.getAttribute('data-src-base64') || img.src;
            if (!src.startsWith('data:')) {
              try {
                var canvas = document.createElement('canvas');
                canvas.width = img.naturalWidth || img.width;
                canvas.height = img.naturalHeight || img.height;
                var ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0);
                src = canvas.toDataURL('image/png');
              } catch (e) {
                console.warn('Could not convert image to base64', e);
                src = null;
              }
            }
            if (!src) return;
            cols.push({ image: src, width: 160, margin: [0, 0, 5, 5] });
            processed++;
            if (processed % 3 === 0) {
              photoBlocks.push({ columns: cols });
              cols = [];
            }
          });
          if (cols.length) {
            photoBlocks.push({ columns: cols });
          }
          if (photoBlocks.length) {
            photoBlocks.unshift({ text: 'AREA / ITEM PHOTOS', style: 'rowLabel', margin: [0, 12, 0, 4] });
            return photoBlocks;
          }
          return null;
        })(),
        { text: 'CATATAN', style: 'rowLabel', margin: [0, 12, 0, 4] },
        { table: notesTable, layout: descLayout }
      ].filter(Boolean)
    };
  }

  function collectSections() {
    return [buildApprovalSection()];
  }

  function createDocumentDefinition() {
    return {
      pageSize: 'A4',
      pageMargins: [20, 20, 20, 20],
      content: collectSections(),
      defaultStyle: { fontSize: 9 },
      styles: {
        tableHeader: { bold: true, alignment: 'center' },
        rowLabel: { bold: true },
        tableBlock: { margin: [0, 6, 0, 12] },
        mainTitle: { fontSize: 25, bold: true },
        subTitle: { fontSize: 9 }
      }
    };
  }

  function exportPdf() {
    if (typeof pdfMake === 'undefined') {
      alert('pdfmake belum dimuat');
      return;
    }
    pdfMake.createPdf(createDocumentDefinition()).open();
  }

  return { exportPdf: exportPdf, setCompanyLogo: setCompanyLogo };
})();
