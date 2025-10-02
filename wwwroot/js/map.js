// (function () {
//   let map = null;
//   let marker = null;
//   let selection = null;

//   function ensureReady(callback) {
//     if (window.google && google.maps) { callback(); return; }
//     let n = 0;
//     const id = setInterval(() => {
//       if (window.google && google.maps) {
//         clearInterval(id);
//         callback();
//       } else if (++n > 100) {
//         clearInterval(id);
//         console.warn("Google Maps API not loaded");
//       }
//     }, 100);
//   }

//   function init(elementId, lat, lng, zoom) {
//     ensureReady(() => {
//       const el = document.getElementById(elementId);
//       if (!el) return;

//       const center = { lat: lat || 0, lng: lng || 0 };
//       map = new google.maps.Map(el, {
//         center,
//         zoom: zoom || 8,
//         streetViewControl: false,
//         mapTypeControl: false,
//         fullscreenControl: false,
//       });

//       marker = new google.maps.Marker({ map, position: center });
//       selection = { lat: center.lat, lng: center.lng };

//       map.addListener("click", (e) => {
//         selection = { lat: e.latLng.lat(), lng: e.latLng.lng() };
//         if (marker) marker.setPosition(selection);
//         else marker = new google.maps.Marker({ map, position: selection });
//       });

//       window.addEventListener("resize", () => {
//         if (!map) return;
//         google.maps.event.trigger(map, "resize");
//       });
//     });
//   }

//   function setCenter(lat, lng, zoom) {
//     ensureReady(() => {
//       if (!map) return;
//       const pos = { lat, lng };
//       map.setCenter(pos);
//       if (zoom) map.setZoom(zoom);
//       if (!marker) marker = new google.maps.Marker({ map, position: pos });
//       else marker.setPosition(pos);
//       selection = { lat, lng };
//     });
//   }

//   function getSelection() {
//     if (!selection) return null;
//     return { Latitude: selection.lat, Longitude: selection.lng };
//   }

//   window.gmap = { init, setCenter, getSelection };
// })();
