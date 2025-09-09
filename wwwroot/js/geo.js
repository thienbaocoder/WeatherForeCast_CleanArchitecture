window.weather = {
  getCurrentPosition: () => new Promise((resolve, reject) => {
    if (!('geolocation' in navigator)) {
      reject('Trình duyệt không hỗ trợ Geolocation');
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        resolve({
          Latitude: pos.coords.latitude,
          Longitude: pos.coords.longitude
        });
      },
      (err) => {
        reject(err && err.message ? err.message : 'Không thể lấy vị trí');
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
    );
  })
};
