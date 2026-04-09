package com.ordermanager.order.repository;

import com.ordermanager.order.model.CustomerOrder;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface OrderRepository extends JpaRepository<CustomerOrder, Integer> {

    List<CustomerOrder> findByCustomerId(Integer customerId);
}
