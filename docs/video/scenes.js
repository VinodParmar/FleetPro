// Scenes of the FleetPro Hindi walkthrough.
// Each scene: { id, title, role: 'anon'|'tenant'|'admin', setup?(h), beats: [{ say: 'Hindi narration', do?: async h => {...} }] }
// or an image scene: { id, title, image, beats: [{ say }] }.
// IMPORTANT: actions never press Save/Add/Delete — forms are filled for the camera and then cancelled,
// so recording does not change any data.
const path = require('path');
const asset = n => path.join(__dirname, 'assets', n);

module.exports = [
  // ─────────────────────────────────────────────── 0. Intro
  {
    id: 'intro', title: 'FleetPro — परिचय', image: asset('intro.png'),
    beats: [
      { say: 'नमस्ते! इस वीडियो में हम FleetPro को शुरू से आख़िर तक, हर स्क्रीन खोलकर विस्तार से देखेंगे।' },
      { say: 'FleetPro आपके ट्रांसपोर्ट कारोबार का पूरा हिसाब — ट्रक, ड्राइवर, ट्रिप, खर्च और पेमेंट — एक ही जगह रखता है।' },
    ],
  },
  {
    id: 'flow', title: 'काम कैसे होता है', image: asset('steps.png'),
    beats: [
      { say: 'काम का तरीका बहुत आसान है। पहले, ट्रक और ड्राइवर एक बार जोड़ दीजिए।' },
      { say: 'फिर हर ट्रिप बनाइए, उसका खर्च और पेमेंट लिखते जाइए।' },
      { say: 'और मुनाफ़े का पूरा हिसाब FleetPro अपने-आप निकाल देता है। चलिए, लॉगिन से शुरू करते हैं।' },
    ],
  },

  // ─────────────────────────────────────────────── 1. Login
  {
    id: 'login', title: 'लॉगिन', role: 'anon',
    setup: async h => {
      await h.go('/Account/Login');
      // Never show the demo-credentials box on camera
      await h.page.evaluate(() => [...document.querySelectorAll('div')]
        .filter(d => d.textContent.includes('Demo Credentials') && d.querySelectorAll('div').length === 0)
        .forEach(d => d.remove()));
    },
    beats: [
      { say: 'यह FleetPro की लॉगिन स्क्रीन है। आपके हर स्टाफ़ को अपना अलग ईमेल और पासवर्ड मिलता है।', do: h => h.spot('.login-box, .card, form') },
      { say: 'अपना ईमेल और पासवर्ड डालिए।', do: async h => { await h.unspot(); await h.type('#Email', process.env.FP_TENANT_EMAIL, { delay: 45 }); await h.type('#Password', process.env.FP_TENANT_PASSWORD, { delay: 45 }); } },
      { say: '"मुझे याद रखें" चुनने पर इस कंप्यूटर पर बार-बार लॉगिन नहीं करना पड़ता।', do: h => h.spot('#remember') },
      { say: 'अब साइन इन दबाइए — और आप सीधे डैशबोर्ड पर पहुँच जाते हैं।', do: async h => { await h.unspot(); await h.click('button[type=submit]', { nav: true }); } },
      { say: 'एक और बात — अगर स्क्रीन कुछ देर खाली छोड़ दें, तो वह अपने-आप लॉक हो जाती है। इससे आपका हिसाब सुरक्षित रहता है।' },
    ],
  },

  // ─────────────────────────────────────────────── 2. Dashboard
  {
    id: 'dashboard', title: 'डैशबोर्ड', role: 'tenant',
    setup: h => h.go('/Dashboard'),
    beats: [
      { say: 'यह डैशबोर्ड है — आपके पूरे कारोबार की तस्वीर, एक नज़र में।' },
      { say: 'सबसे ऊपर तीन रंगीन बॉक्स हैं — कितने ट्रक चालू हैं, कितनी ट्रिप अभी चल रही हैं, और आपके पास कुल कितने ड्राइवर हैं।', do: h => h.spot('.small-box', { all: true }) },
      { say: 'इनके नीचे इस महीने का हिसाब है — कुल आय, कुल खर्च, और शुद्ध लाभ। यह सब आपकी ट्रिप से अपने-आप जुड़ता है, यहाँ कुछ भरना नहीं पड़ता।', do: h => h.spot('.info-box', { all: true }) },
      { say: 'यह चार्ट पिछले छह महीने की आय और खर्च दिखाता है। नीली लाइन बताती है कि हर महीने कितना मुनाफ़ा हुआ।', do: h => h.spot(['.card', 'आय बनाम व्यय']) },
      { say: 'बगल वाला गोल चार्ट ट्रिप की स्थिति बताता है — कितनी पूरी हुईं, कितनी चल रही हैं, और कितनी आगे के लिए तय हैं।', do: h => h.spot(['.card', 'यात्रा स्थिति']) },
      { say: 'नीचे देखें तो — खर्च किस चीज़ पर हुआ, ट्रकों की स्थिति, और सबसे ज़रूरी अलर्ट, एक साथ दिखते हैं।', do: async h => { await h.unspot(); await h.scrollBy(420); await h.spot(['.card', 'श्रेणी अनुसार व्यय'], { move: false }); await h.wait(1600); await h.spot(['.card', 'तत्काल अलर्ट']); } },
      { say: 'और सबसे नीचे हाल की ट्रिप की सूची है, जिसमें हर ट्रिप की रकम और स्थिति दिखती है।', do: async h => { await h.unspot(); await h.scrollBy(500); await h.spot('table'); } },
      { say: 'बाईं तरफ़ मेन्यू है — संचालन, वाहन बेड़ा, भागीदार, रिपोर्ट और प्रशासन। चलिए एक-एक करके देखते हैं।', do: async h => { await h.unspot(); await h.scrollTop(); await h.spot('.nav-sidebar'); } },
      { say: 'ऊपर दाईं ओर हि और En बटन से भाषा बदलती है, और घंटी पर लाल नंबर बताता है कि कितने अलर्ट अभी देखने बाकी हैं।', do: h => h.spot([['a[href="?lang=hi"]'], ['a[href="?lang=en"]'], ['.fa-bell']], { many: true }) },
    ],
  },

  // ─────────────────────────────────────────────── 3. Trucks
  {
    id: 'trucks', title: 'ट्रक', role: 'tenant',
    setup: h => h.go('/Dashboard'),
    beats: [
      { say: 'अब वाहन बेड़ा में "ट्रक" खोलते हैं।', do: h => h.nav('/Truck') },
      { say: 'ऊपर गिनती है — कुल ट्रक, चालू ट्रक, मरम्मत में गए ट्रक, और कितने ट्रकों के कागज़ों पर ध्यान देना है।', do: h => h.spot('.info-box', { all: true }) },
      { say: 'नीचे हर ट्रक की एक लाइन है — नंबर प्लेट, मॉडल, साल, फ़िटनेस और बीमा की आख़िरी तारीख़, और स्थिति।', do: h => h.spot('table') },
      { say: 'लाल रंग और चेतावनी का निशान मतलब कागज़ खत्म हो चुका है। हरा रंग मतलब कागज़ चालू है, और साथ में लिखा है कि कितने दिन बाकी हैं।', do: async h => { await h.spot(['td', 'EXPIRED']); await h.wait(2500); await h.spot('td .text-success, td.expiry-ok, td .expiry-ok'); } },
      { say: 'ऊपर के सर्च बॉक्स में नंबर प्लेट या मॉडल लिखकर कोई भी ट्रक तुरंत ढूँढ सकते हैं।', do: async h => { await h.unspot(); await h.type('input[name=search]', 'Tata'); await h.page.keyboard.press('Enter'); await h.page.waitForNavigation({ waitUntil: 'networkidle2' }).catch(() => {}); await h.ready(); await h.spot('table'); } },
      { say: 'नया ट्रक जोड़ने के लिए "ट्रक जोड़ें" बटन दबाइए।', do: async h => { await h.unspot(); await h.click('a[href="/Truck/Create"]', { nav: true }); } },
      { say: 'फ़ॉर्म में नंबर प्लेट और मॉडल ज़रूरी हैं। कंपनी, साल, इंजन और चेसिस नंबर भी भर सकते हैं।', do: async h => { await h.type('#NumberPlate', 'MH12AB4321'); await h.type('#Model', 'Tata Signa 4825'); await h.type('#ManufacturingYear', '2024'); } },
      { say: 'दाईं तरफ़ कागज़ों की तारीख़ें हैं — फ़िटनेस, बीमा, रोड टैक्स, परमिट, नेशनल परमिट और PUC।', do: h => h.spot(['.card', 'समाप्ति तिथियां']) },
      { say: 'ये तारीख़ें डालने के बाद FleetPro खुद इन पर नज़र रखता है, और खत्म होने से पहले अलर्ट दिखाता है।', do: async h => { await h.unspot(); await h.setValue('#FitnessExpiry', '2027-03-15'); await h.setValue('#InsuranceExpiry', '2027-01-20'); } },
      { say: 'सब भरकर हरा बटन दबाते ही ट्रक आपकी सूची में आ जाता है। अभी हम इसे रद्द करके आगे बढ़ते हैं।', do: async h => { await h.spot('button[type=submit].btn-success'); await h.wait(1800); await h.unspot(); await h.click('a.btn-secondary[href="/Truck"]', { nav: true }); } },
    ],
  },

  // ─────────────────────────────────────────────── 4. Drivers & agents
  {
    id: 'drivers', title: 'ड्राइवर और एजेंट', role: 'tenant',
    setup: h => h.go('/Truck'),
    beats: [
      { say: 'अब "ड्राइवर" खोलते हैं। यहाँ आपके सभी ड्राइवरों की पूरी जानकारी है।', do: h => h.nav('/Driver') },
      { say: 'हर ड्राइवर का नाम, फ़ोन, लाइसेंस नंबर, लाइसेंस का प्रकार, उसकी आख़िरी तारीख़, और मासिक तनख़्वाह दिखती है।', do: h => h.spot('table') },
      { say: 'जिस ड्राइवर का लाइसेंस खत्म हो गया है, वह लाल रंग में दिखता है — ताकि बिना वैध लाइसेंस के गाड़ी न चले।', do: h => h.spot(['td', 'EXPIRED']) },
      { say: '"ड्राइवर जोड़ें" से नया ड्राइवर जोड़ते समय बैंक खाता और IFSC भी डाल सकते हैं, जिससे तनख़्वाह देना आसान हो जाता है।', do: h => h.spot('a[href="/Driver/Create"]') },
      { say: 'अब "भागीदार" में एजेंट देखते हैं — यानी वे ब्रोकर या पार्टियाँ जो आपको माल देती हैं।', do: h => h.nav('/Agent') },
      { say: 'हर एजेंट का नाम, कंपनी और फ़ोन नंबर यहाँ रहता है। ट्रिप बनाते समय इन्हीं में से एजेंट चुना जाता है।', do: h => h.spot('table') },
      { say: 'नया एजेंट जोड़ने पर एक छोटा फ़ॉर्म खुलता है — नाम, कंपनी और फ़ोन भरिए, बस।', do: async h => { await h.unspot(); await h.click('button[onclick="openAgentModal()"]'); await h.wait(900); await h.spot('#agentModal .modal-content'); await h.wait(2200); await h.unspot(); await h.click('#agentModal .btn-secondary'); } },
    ],
  },

  // ─────────────────────────────────────────────── 5. Trips list + new trip
  {
    id: 'trips', title: 'ट्रिप', role: 'tenant',
    setup: h => h.go('/Agent'),
    beats: [
      { say: 'अब सबसे ज़रूरी हिस्सा — ट्रिप। "संचालन" में "यात्राएं" खोलिए।', do: h => h.nav('/Trip') },
      { say: 'ऊपर कुल ट्रिप, चल रही ट्रिप, पूरी हुई ट्रिप, और कुल सौदे की रकम दिखती है।', do: h => h.spot('.info-box', { all: true }) },
      { say: 'हर लाइन एक ट्रिप है — रूट, ड्राइवर, ट्रक, तारीख़, किलोमीटर, सौदे की रकम, खर्च, और मुनाफ़ा।', do: h => h.spot('table') },
      { say: 'सबसे नीचे सबका जोड़ है — यानी कुल कमाई, कुल खर्च, और कुल मुनाफ़ा, एक नज़र में।', do: h => h.spot('tfoot') },
      { say: 'ऊपर तारीख़ और स्थिति से ट्रिप छाँट सकते हैं, और हरे "निर्यात" बटन से पूरी सूची Excel में डाउनलोड हो जाती है।', do: h => h.spot('form.form-inline') },
      { say: 'हर ट्रिप के आगे तीन बटन हैं — पूरा देखना, बदलना, और सीधे खर्च जोड़ना।', do: h => h.spot('tbody tr td:first-child') },
      { say: 'नई ट्रिप बनाना बहुत आसान है। "नई यात्रा" दबाइए।', do: async h => { await h.unspot(); await h.click('a.btn-primary[href="/Trip/Create"]', { nav: true }); } },
      { say: 'ट्रक और ड्राइवर चुनिए, और ज़रूरत हो तो माल या रूट के बारे में कुछ नोट लिख दीजिए।', do: async h => { await h.choose('#TruckId', 1); await h.choose('#DriverId', 1); await h.type('#Notes', 'Delhi से Jaipur, FMCG माल', { delay: 55 }); } },
      { say: 'सेव करते ही ट्रिप नंबर अपने-आप बन जाता है, और आप ट्रिप के पूरे पेज पर पहुँच जाते हैं — जहाँ माल, पेमेंट और खर्च भरे जाते हैं। चलिए, एक बनी हुई ट्रिप खोलकर देखते हैं।', do: async h => { await h.spot('button[type=submit]'); await h.wait(2500); await h.unspot(); await h.go('/Trip'); } },
    ],
  },

  // ─────────────────────────────────────────────── 6. Trip page: up/down phase
  {
    id: 'trip-detail', title: 'ट्रिप का पूरा पेज', role: 'tenant',
    setup: h => h.go('/Trip'),
    beats: [
      { say: 'किसी भी ट्रिप के पीले बटन से उसका पूरा पेज खुलता है।', do: h => h.click('a[href*="/Trip/Edit"]', { nav: true }) },
      { say: 'बाईं तरफ़ ट्रिप नंबर, ट्रक और ड्राइवर हैं, और यहीं से ट्रिप की स्थिति बदल सकते हैं — तय, चल रही, या पूरी।', do: h => h.spot(['.card', 'Trip Info']) },
      { say: 'दाईं तरफ़ चार टैब हैं — अप चरण, डाउन चरण, पेमेंट, और खर्च।', do: h => h.spot('.nav-tabs') },
      { say: '"अप चरण" मतलब जाने वाला माल। कहाँ से कहाँ, कब निकले और कब पहुँचे — यह सब यहाँ भरते हैं।', do: h => h.spot([['#tabUp [name="UpPhase.FromLocation"]'], ['#tabUp [name="UpPhase.EndDate"]']], { many: true }) },
      { say: 'किस एजेंट से माल मिला, बिल्टी यानी LR नंबर क्या है, और माल क्या है — यह भी लिखा जाता है।', do: h => h.spot([['#tabUp [name="UpPhase.AgentId"]'], ['#tabUp [name="UpPhase.CargoDescription"]']], { many: true }) },
      { say: 'अब ध्यान से देखिए — नेट वज़न और रेट डालते ही सौदे की रकम अपने-आप निकल आती है। यहाँ बारह सौ गुणा सौ, यानी एक लाख बीस हज़ार रुपये।', do: async h => { await h.unspot(); await h.type('#tabUp [name="UpPhase.NetWeight"]', '1200'); await h.type('#tabUp [name="UpPhase.Rate"]', '100'); await h.spot('#tabUp .calc-deal'); } },
      { say: 'इसी तरह शुरू और आख़िरी मीटर रीडिंग डालने पर कुल किलोमीटर खुद गिन लिए जाते हैं।', do: h => h.spot([['#tabUp [name="UpPhase.StartMeterReading"]'], ['#tabUp [name="UpPhase.TareWeight"]']], { many: true }) },
      { say: 'बाईं तरफ़ का सारांश हर बदलाव के साथ अपडेट होता है — कुल किलोमीटर, सौदे की रकम, मिला पैसा, खर्च, बाकी रकम, और मुनाफ़ा।', do: h => h.spot(['.card', 'Summary']) },
      { say: '"डाउन चरण" में वापसी का माल भरते हैं — ताकि ट्रक खाली न लौटे, और वापसी की कमाई भी इसी ट्रिप में जुड़ जाए।', do: async h => { await h.unspot(); await h.click('a[href="#tabDown"]'); await h.wait(600); await h.spot('#tabDown'); } },
    ],
  },

  // ─────────────────────────────────────────────── 7. Payments
  {
    id: 'payments', title: 'पेमेंट', role: 'tenant',
    setup: async h => {
      await h.go('/Trip');
      const hrefs = await h.page.$$eval('a[href*="/Trip/Edit"]', as => as.map(a => a.getAttribute('href')));
      await h.go(hrefs[hrefs.length - 1]); // oldest trip has payments
    },
    beats: [
      { say: 'अब पेमेंट देखते हैं। "पेमेंट" टैब में इस ट्रिप का हर लेन-देन लिखा है।', do: async h => { await h.click('a[href="#tabPayments"]'); await h.wait(500); } },
      { say: 'हरे रंग में "प्राप्त" — यानी पार्टी से मिला पैसा। लाल रंग में "भुगतान" — यानी आपने किसी को दिया, जैसे ड्राइवर को एडवांस।', do: h => h.spot('#tabPayments table') },
      { say: 'हर पेमेंट के साथ तारीख़, तरीका — नकद, UPI या बैंक — और किससे मिला या किसे दिया, यह भी दर्ज रहता है। आख़िरी कॉलम में चालू बैलेंस है।', do: h => h.spot('#tabPayments table tbody') },
      { say: 'नया पेमेंट जोड़ना हो, तो प्रकार चुनिए, रकम, तारीख़, तरीका और पार्टी का नाम भरिए, और "जोड़ें" दबाइए।', do: async h => { await h.unspot(); await h.choose('#tabPayments [name=PaymentType]', 1); await h.type('#tabPayments [name=Amount]', '2400'); await h.choose('#tabPayments [name=PaymentMode]', 3); await h.type('#tabPayments [name=PayerPayee]', 'ABC Logistics', { delay: 55 }); await h.spot('#tabPayments form button[type=submit]'); } },
      { say: 'सारांश में "Pending" यानी बाकी रकम बताती है कि पार्टी से अभी कितना लेना है। पूरा पैसा आते ही यह शून्य हो जाता है।', do: h => h.spot(['.card', 'Summary']) },
      { say: '"खर्च" टैब में इस ट्रिप के सारे खर्च एक जगह दिखते हैं — डीज़ल, टोल, और बाकी सब।', do: async h => { await h.unspot(); await h.click('a[href="#tabExpenses"]'); await h.wait(600); await h.spot('#tabExpenses'); } },
    ],
  },

  // ─────────────────────────────────────────────── 8. Expenses
  {
    id: 'expenses', title: 'खर्च', role: 'tenant',
    setup: h => h.go('/Trip'),
    beats: [
      { say: 'अब "खर्चे" पेज खोलते हैं। यहाँ सभी ट्रिप के सारे खर्च एक साथ हैं।', do: h => h.nav('/Expense') },
      { say: 'हर खर्च के साथ ट्रिप, प्रकार, रकम, तारीख़, दुकान या पंप का नाम, और बिल नंबर दिखता है।', do: h => h.spot('table') },
      { say: 'नया खर्च जोड़ने के लिए "खर्चा जोड़ें" दबाइए — एक छोटा फ़ॉर्म खुलता है।', do: async h => { await h.unspot(); await h.click('button[onclick="openAddExpenseModal()"]'); await h.wait(800); } },
      { say: 'पहले ट्रिप चुनिए, फिर खर्च का प्रकार — जैसे डीज़ल, टोल, मरम्मत या खाना।', do: async h => { await h.choose('#modalTripId', 1); await h.choose('#modalCategory', 1); } },
      { say: 'फिर रकम, विवरण और पंप या दुकान का नाम भरिए।', do: async h => { await h.type('#modalAmount', '8500'); await h.type('#modalDescription', 'डीज़ल 100 लीटर', { delay: 60 }); await h.type('#modalVendorName', 'HP Petrol Pump', { delay: 50 }); } },
      { say: 'और सबसे काम की चीज़ — बिल या रसीद की फ़ोटो यहीं लगा सकते हैं, ताकि बाद में हिसाब में कोई शक न रहे।', do: h => h.spot('#modalReceiptFile') },
      { say: '"खर्चा जोड़ें" दबाते ही खर्च उस ट्रिप में जुड़ जाता है, और ट्रिप का मुनाफ़ा दोबारा निकल आता है। अभी हम इसे रद्द करते हैं।', do: async h => { await h.spot('#expenseForm button[type=submit]'); await h.wait(2200); await h.unspot(); await h.click('#expenseForm .btn-secondary[data-dismiss="modal"]'); } },
    ],
  },

  // ─────────────────────────────────────────────── 9. Alerts
  {
    id: 'alerts', title: 'अलर्ट', role: 'tenant',
    setup: h => h.go('/Expense'),
    beats: [
      { say: 'अब अलर्ट देखते हैं — FleetPro का सबसे मददगार हिस्सा।', do: h => h.nav('/Alert') },
      { say: 'ऊपर चार बॉक्स हैं — गंभीर यानी सात दिन के अंदर, चेतावनी यानी आठ से तीस दिन, आगामी यानी इकतीस से साठ दिन, और जो अलर्ट अभी पढ़े नहीं गए।', do: h => h.spot('.info-box', { all: true }) },
      { say: 'लाल हिस्से में वे कागज़ हैं जो खत्म हो चुके हैं या बस खत्म होने वाले हैं — ट्रक का बीमा, फ़िटनेस, या ड्राइवर का लाइसेंस।', do: h => h.spot('.card.card-danger, .card:has(.bg-danger)') },
      { say: 'कागज़ नया बनवा लिया? तो यह निशान दबाकर अलर्ट को "पढ़ा हुआ" कर दीजिए।', do: h => h.spot('form[action*="MarkRead"] button') },
      { say: '"अलर्ट ताज़ा करें" दबाने पर FleetPro सारी तारीख़ें दोबारा जाँच लेता है। ऊपर घंटी पर भी यही गिनती दिखती है।', do: h => h.spot([['form[action*="Refresh"] button'], ['.fa-bell']], { many: true }) },
    ],
  },

  // ─────────────────────────────────────────────── 10. Reports
  {
    id: 'reports', title: 'लाभ-हानि रिपोर्ट', role: 'tenant',
    setup: h => h.go('/Alert'),
    beats: [
      { say: '"रिपोर्ट" में आपके कारोबार का पूरा हिसाब है।', do: h => h.nav('/Report') },
      { say: 'लाभ-हानि रिपोर्ट के लिए शुरू और आख़िरी तारीख़ चुनिए — एक महीना, तीन महीने, या पूरा साल।', do: async h => { await h.spot('form[action*="PL"]'); await h.wait(1200); await h.unspot(); await h.setValue('form[action*="PL"] input[name=FromDate]', `${new Date().getFullYear()}-01-01`); await h.setValue('form[action*="PL"] input[name=ToDate]', `${new Date().getFullYear()}-12-31`); } },
      { say: 'और "रिपोर्ट बनाएं" दबाइए।', do: h => h.click('form[action*="PL"] button[type=submit]', { nav: true }) },
      { say: 'हरे हिस्से में कुल आय है।', do: h => h.spot(['.card', 'Revenue']) },
      { say: 'लाल हिस्से में खर्च है, अलग-अलग — डीज़ल, मरम्मत, टोल, मज़दूरी, और बाकी।', do: h => h.spot(['.card', 'Expenses']) },
      { say: 'नीले हिस्से में शुद्ध लाभ और मार्जिन है — यानी हर सौ रुपये की कमाई में से कितने बचे।', do: h => h.spot(['.card', 'Net Profit']) },
      { say: 'नीचे हर ट्रिप का अलग हिसाब है, ताकि पता चले कि कौन-सा रूट सबसे ज़्यादा फ़ायदा देता है।', do: h => h.spot(['.card', 'Trip-wise']) },
      { say: 'ऊपर "Print" और "Export Excel" बटन से यह रिपोर्ट अपने अकाउंटेंट या पार्टनर को भेज सकते हैं।', do: h => h.spot([['button[onclick="window.print()"]'], ['a[href*="ExportTripsExcel"]']], { many: true }) },
    ],
  },

  // ─────────────────────────────────────────────── 11. Users & permissions
  {
    id: 'users', title: 'स्टाफ़ और अनुमतियाँ', role: 'tenant',
    setup: h => h.go('/Dashboard'),
    beats: [
      { say: '"प्रशासन" में "उपयोगकर्ता" खोलिए। यहाँ आपके स्टाफ़ के लॉगिन हैं।', do: h => h.nav('/User') },
      { say: 'हर व्यक्ति का नाम, ईमेल, भूमिका, और स्थिति दिखती है।', do: h => h.spot('table') },
      { say: 'नया स्टाफ़ जोड़कर उसकी भूमिका चुनिए — एडमिन यानी सब कुछ, डेटा एंट्री यानी जोड़ना और बदलना, या व्यूअर यानी सिर्फ़ देखना।', do: h => h.spot('a[href="/User/Create"]') },
      { say: 'चाबी वाले बटन से किसी एक व्यक्ति के अधिकार खोलते हैं।', do: async h => { await h.unspot(); const btns = await h.page.$$('a[href*="/User/Permissions"]'); await h.click(btns[btns.length - 1], { nav: true }); } },
      { say: 'हर काम के लिए तीन विकल्प हैं — Grant मतलब हमेशा अनुमति, Role मतलब भूमिका के हिसाब से, और Deny मतलब हमेशा रोक।', do: h => h.spot('.card') },
      { say: 'जैसे, किसी डेटा एंट्री वाले को ट्रक जोड़ने दे सकते हैं, पर हटाने से रोक सकते हैं। इससे हर व्यक्ति सिर्फ़ उतना ही कर पाता है, जितनी आपने अनुमति दी है।', do: h => h.spot(['tbody tr', 'Trucks']) },
    ],
  },

  // ─────────────────────────────────────────────── 12. Audit log
  {
    id: 'audit', title: 'हर बदलाव का रिकॉर्ड', role: 'tenant',
    setup: h => h.go('/User'),
    beats: [
      { say: '"ऑडिट लॉग" में हर बदलाव का रिकॉर्ड है।', do: h => h.nav('/Audit') },
      { say: 'किसने, कब, क्या जोड़ा, बदला या हटाया — सब अपने-आप दर्ज होता है। आपको कुछ अलग से लिखना नहीं पड़ता।', do: h => h.spot('table') },
      { say: 'आँख वाले बटन से पूरा ब्यौरा दिखता है — पहले क्या था और बाद में क्या हुआ।', do: async h => { await h.unspot(); await h.click('button[data-toggle="modal"]'); await h.wait(900); await h.spot('.modal.show .modal-content'); await h.wait(2600); await h.unspot(); await h.click('.modal.show .btn-secondary'); } },
      { say: 'ऊपर से हिस्सा और तारीख़ चुनकर कोई भी पुराना बदलाव खोज सकते हैं।', do: h => h.spot('form') },
    ],
  },

  // ─────────────────────────────────────────────── 13. Language
  {
    id: 'language', title: 'हिंदी और अंग्रेज़ी', role: 'tenant',
    setup: h => h.go('/Dashboard'),
    beats: [
      { say: 'FleetPro हिंदी और अंग्रेज़ी, दोनों भाषाओं में चलता है।', do: h => h.spot([['a[href="?lang=hi"]'], ['a[href="?lang=en"]']], { many: true }) },
      { say: 'En दबाइए — पूरा ऐप अंग्रेज़ी में बदल जाता है।', do: async h => { await h.unspot(); await h.click('a[href="?lang=en"]', { nav: true }); } },
      { say: 'और हि दबाइए — फिर से हिंदी में। हर व्यक्ति की पसंद अगली बार के लिए याद रहती है।', do: h => h.click('a[href="?lang=hi"]', { nav: true }) },
    ],
  },

  // ─────────────────────────────────────────────── 14. Platform owner
  {
    id: 'companies', title: 'कई कंपनियाँ, एक प्लेटफ़ॉर्म', role: 'admin',
    setup: h => h.go('/Tenant'),
    beats: [
      { say: 'आख़िर में, प्लेटफ़ॉर्म के मालिक की स्क्रीन। यहाँ FleetPro इस्तेमाल करने वाली सभी ट्रांसपोर्ट कंपनियाँ हैं।', do: h => h.spot('.info-box', { all: true }) },
      { say: 'हर कंपनी का मालिक, प्लान, ट्रक और यूज़र की सीमा, और स्थिति यहाँ दिखती है।', do: h => h.spot('table') },
      { say: 'हर कंपनी का डेटा पूरी तरह अलग और निजी रहता है — एक कंपनी दूसरी कंपनी का कुछ भी नहीं देख सकती।' },
    ],
  },

  // ─────────────────────────────────────────────── 15. Outro
  {
    id: 'outro', title: 'धन्यवाद', image: asset('outro.png'),
    beats: [
      { say: 'तो यह था FleetPro — ट्रक, ड्राइवर, ट्रिप, पेमेंट और खर्च, सब एक जगह।' },
      { say: 'मुनाफ़े का हिसाब अपने-आप, कागज़ों के अलर्ट समय पर, और रिपोर्ट एक क्लिक में।' },
      { say: 'धन्यवाद! कोई भी सवाल हो, तो हमसे ज़रूर पूछिए।' },
    ],
  },
];
